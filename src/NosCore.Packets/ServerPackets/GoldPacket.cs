using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("gold")]
    public class GoldPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public long Gold { get; set; }

        [PacketIndex(1)]
        public int Unknown { get; set; }
    }
}
