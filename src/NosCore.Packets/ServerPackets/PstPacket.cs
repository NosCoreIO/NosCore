using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("pst")]
    public class PstPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public VisualType Type { get; set; }

        [PacketIndex(1)]
        public long VisualId { get; set; }

        [PacketIndex(2)]
        public int GroupOrder { get; set; }

        [PacketIndex(3)]
        public int HpLeft { get; set; }

        [PacketIndex(4)]
        public int MpLeft { get; set; }

        [PacketIndex(5)]
        public int HpLoad { get; set; }

        [PacketIndex(6)]
        public int MpLoad { get; set; }

        [PacketIndex(7)]
        public CharacterClassType Class { get; set; }

        [PacketIndex(8)]
        public GenderType Gender { get; set; }

        [PacketIndex(9)]
        public short Morph { get; set; }

        [PacketIndex(10, IsOptional = true)]
        public string Buffs { get; set; } //TODO: Change this into a list of buffs
    }
}
