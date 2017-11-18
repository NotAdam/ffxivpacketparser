using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ffxivpacketparser
{
    public class PipeServer
    {
        public sealed class PacketEventArgs : EventArgs
        {
            public enum PacketDirection
            {
                LobbySend,
                LobbyRecv,
                GameSend,
                GameRecv
            }

            public readonly byte[] Data;
            public readonly int Length;
            public readonly int Port;
            public readonly PacketDirection Direction;

            public PacketEventArgs( byte[] data, int length, int port, PacketDirection direction )
            {
                Data = data;
                Length = length;
                Port = port;
                Direction = direction;
            }

            public override string ToString()
            {
                return $"Packet\n -> length: {Length.ToString().PadLeft(4)}, Port: {Port}, Direction: {Direction}";
            }
        }

        public const int LOBBY_SERVER_PORT = 54994;

        public delegate void PacketRecievedHandler( object sender, PacketEventArgs e );
        public delegate void PacketSentHandler( object sender, PacketEventArgs e );
        public event PacketRecievedHandler OnPacketRecieved;
        public event PacketSentHandler OnPacketSent;

        public readonly string PipeName = null;

        protected bool ShouldFinishListening = false;

        public PipeServer( string name = "FFXIVMon" )
        {
            PipeName = name;
        }

        public void Listen()
        {
            ShouldFinishListening = false;

            var task = new Task(() =>
            {
                while ( !ShouldFinishListening )
                {
                    var server = new NamedPipeServerStream( PipeName, PipeDirection.InOut );

                    server.WaitForConnection();

                    char[] type = null;
                    int port = 0;
                    int len = 0;
                    byte[] packet = null;

                    try
                    {
                        // Pipe data structure
                        // +---------------------------------------------------+
                        // | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | RECV_LEN  |
                        // +---------------------------------------------------+
                        // |  TYPE MAGIC   | PORT  |    RECV_LEN   | BUFFER    |
                        // +---------------------------------------------------+

                        using ( var br = new BinaryReader( server ) )
                        {
                            type = br.ReadChars( 4 ); // 0 to 3
                            port = br.ReadUInt16(); // 4 to 5
                            len = br.ReadInt32(); // 6 to 9
                            packet = br.ReadBytes( len ); // 10..*
                        }
                    }
                    catch ( IOException ex )
                    {
                        
                    }

                    ProcessPacket( type, port, len, packet );

                    server.Close();
                }
            });

            task.Start();
        }

        public void ProcessPacket( char[] type, int port, int len, byte[] packet )
        {
            PacketEventArgs.PacketDirection direction;

            if ( type[ 0 ] == 'R' )
            {
                direction = PacketEventArgs.PacketDirection.GameRecv;
                if ( port == LOBBY_SERVER_PORT )
                {
                    direction = PacketEventArgs.PacketDirection.LobbyRecv;
                }
            }
            else
            {
                direction = PacketEventArgs.PacketDirection.GameSend;
                if ( port == LOBBY_SERVER_PORT )
                {
                    direction = PacketEventArgs.PacketDirection.LobbySend;
                }
            }

            var packetEvent = new PacketEventArgs( packet, len, port, direction );

            if ( type[ 0 ] == 'R' )
            {
                OnPacketRecieved?.Invoke( this, packetEvent );
            }
            else
            {
                OnPacketSent?.Invoke( this, packetEvent );
            }
        }
    }
}
