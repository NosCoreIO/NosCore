using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("blinit")]
    public class BlinitPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public List<BlinitSubPacket> SubPackets { get; set; }
    }
}
