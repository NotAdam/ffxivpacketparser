using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace ffxivpacketparser
{
    class Program
    {
        static void Main( string[] args )
        {
            var ps = new PipeServer();

            ps.OnPacketRecieved += PacketHandler;
            ps.OnPacketSent += PacketHandler;

            ps.Listen();

            Console.ReadKey();
        }

        private static unsafe void PacketHandler( object sender, PipeServer.PacketEventArgs e )
        {
            fixed ( byte* ptr = &e.Data[ 0 ] )
            {
                Structures.FFXIVARR_PACKET_HEADER pktHdr = *(Structures.FFXIVARR_PACKET_HEADER*)ptr;

                // ping events, don't care
                if ( pktHdr.timestamp == 0 )
                {
                    return;
                }

                var headerSize = Marshal.SizeOf<Structures.FFXIVARR_PACKET_HEADER>();
                var subpacketSize = e.Data.Length - headerSize;

                Console.WriteLine( $" -> ts: { pktHdr.timestamp.ToString().PadLeft( 13 ) }, size: { pktHdr.size.ToString().PadLeft( 4 ) }, subpacket size: { subpacketSize }, seg count: { pktHdr.count.ToString().PadLeft( 2 ) }, compressed: { pktHdr.isCompressed }" );

                byte[] rawData = new byte[ subpacketSize + 1 ];
                Buffer.BlockCopy( e.Data, headerSize, rawData, 0, subpacketSize );

                if ( pktHdr.isCompressed == 1 )
                {
                    var outStream = new MemoryStream();
                    using ( var ms = new MemoryStream( rawData ) )
                    {
                        ms.Seek( 2, SeekOrigin.Current );

                        using ( var ds = new DeflateStream( ms, CompressionMode.Decompress ) )
                        {
                            byte[] buf = new byte[1024];
                            int len;
                            while ( ( len = ds.Read( buf, 0, buf.Length ) ) > 0 )
                            {
                                outStream.Write( buf, 0, len );
                            }
                        }
                    }

                    rawData = outStream.ToArray();
                }

                // parse packet data
                int packetOffset = 0;
                for ( int i = 0; i < pktHdr.count; i++ )
                {
                    var size = (UInt16)rawData[ packetOffset ];
                    var is_for_self = (UInt32)rawData[ packetOffset + 0x04 ] == (UInt32)e.Data[ packetOffset + 0x08 ];
                    var packet_category = (UInt16)rawData[ packetOffset + 0x0c ];

                    UInt32 packet_timestamp = 0;
                    UInt16 packet_type = 0;
                    UInt16 additional = 0;

                    if ( packet_category == 3 )
                    {
                        packet_type = ( UInt16 )rawData[ packetOffset + 0x12 ];
                        packet_timestamp = ( UInt32 )rawData[ packetOffset + 0x18 ];
                    }

                    if ( size > 0x20 )
                    {
                        additional = ( UInt16 )rawData[ packetOffset + 0x20 ];
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine( $" -> Segment {i}" );
                    sb.AppendLine( $"    -> is_for_self: { is_for_self }, category: { packet_category }, size: {size}" );
                    sb.AppendLine( $"    -> opcode: { packet_type.ToString( "X4" ) }, timestamp: { packet_timestamp }" );

                    Console.WriteLine( sb );

                    // write segment hex dump
                    byte[] seg = new byte[ size ];
                    Buffer.BlockCopy( rawData, packetOffset, seg, 0, size );

                    Console.WriteLine( Util.HexDump( seg ) );

                    packetOffset += size;
                }
            }
        }
    }
}
