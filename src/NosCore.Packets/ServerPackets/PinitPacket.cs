using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("pinit")]
    public class PinitPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public int GroupSize { get; set; }

        [PacketIndex(1, SpecialSeparator = "|")]
        public List<PinitSubPacket> PinitSubPackets { get; set; }
    }
}
