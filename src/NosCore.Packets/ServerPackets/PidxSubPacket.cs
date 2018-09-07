using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("pidx_sub_packet")]
    public class PidxSubPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public bool IsMemberOfGroup { get; set; }

        [PacketIndex(1)]
        public long VisualId { get; set; }
    }
}
