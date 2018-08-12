using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("finit")]
    public class FinitPacket : PacketDefinition
    {
        [PacketIndex(0, SpecialSeparator = "|")]
        public List<FinitSubPacket> SubPackets { get; set; }
    }
}
