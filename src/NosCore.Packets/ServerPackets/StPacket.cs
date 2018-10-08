using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("st")]
    public class StPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public VisualType Type { get; set; }

        [PacketIndex(1)]
        public long VisualId { get; set; }

        [PacketIndex(2)]
        public byte Level { get; set; }

        [PacketIndex(3)]
        public byte HeroLvl { get; set; }

        [PacketIndex(4)]
        public int HpPercentage { get; set; }

        [PacketIndex(5)]
        public int MpPercentage { get; set; }

        [PacketIndex(6)]
        public int CurrentHp { get; set; }

        [PacketIndex(7)]
        public int CurrentMp { get; set; }

        [PacketIndex(8)]
        public List<short> BuffIds { get; set; }
    }
}
