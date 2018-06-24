using NosCore.Core.Serializing;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("stat")]
    public class StatPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public int HP { get; set; }

        [PacketIndex(1)]
        public double HPMaximum { get; set; }

        [PacketIndex(2)]
        public int MP { get; set; }

        [PacketIndex(3)]
        public double MPMaximum { get; set; }

        [PacketIndex(4)]
        public int Unknown { get; set; }

        [PacketIndex(5)]
        public double Option { get; set; }
    }
}
