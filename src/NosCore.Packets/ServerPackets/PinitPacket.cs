using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("pinit")]
    public class PinitPacket : PacketDefinition
    {
        [PacketIndex(0, SpecialSeparator = "|")]
        public List<PinitSubPacket> PinitSubPackets { get; set; }
    }
}
