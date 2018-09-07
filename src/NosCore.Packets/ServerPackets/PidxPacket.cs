using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("pidx")]
    public class PidxPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public long GroupId { get; set; }

        [PacketIndex(1)]
        public List<PidxSubPacket> SubPackets { get; set; }
    }
}
