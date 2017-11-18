using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ffxivpacketparser
{
    public class Structures
    {
        [StructLayout( LayoutKind.Sequential )]
        public struct FFXIVARR_PACKET_HEADER
        {
            UInt64 unknown_0;
            UInt64 unknown_1;

            /// <summary>
            /// Represents the number of milliseconds since epoch that the packet was sent.
            /// </summary>
            public UInt64 timestamp;

            /// <summary>
            /// The size of the packet header and its payload
            /// </summary>
            public UInt32 size;

            UInt16 unknown_C;

            /// <summary>
            /// The number of packet segments that follow.
            /// </summary>
            public UInt16 count;

            byte unknown_10;

            /// <summary>
            /// Indicates if the data segments of this packet are compressed.
            /// </summary>
            public byte isCompressed;

            UInt32 unknown_14;
        }
    }
}
