using JetBrains.Annotations;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("c_info")]
    public class CInfoPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string Name { get; set; }

        [PacketIndex(1)]
        public string Unknown1 { get; set; } //TODO to find

        [PacketIndex(2)]
        public short Unknown2 { get; set; } //TODO to find

        [PacketIndex(3)]
        public int FamilyId { get; set; }

        [PacketIndex(4)]
        public string FamilyName { get; set; }

        [PacketIndex(5)]
        public long CharacterId { get; set; }

        [PacketIndex(6)]
        public byte Authority { get; set; }

        [PacketIndex(7)]
        public byte Gender { get; set; }

        [PacketIndex(8)]
        public byte HairStyle { get; set; }

        [PacketIndex(9)]
        public byte HairColor { get; set; }

        [PacketIndex(10)]
        public byte Class { get; set; }

        [PacketIndex(11)]
        public byte Icon { get; set; }

        [PacketIndex(12)]
        public short Compliment { get; set; }

        [PacketIndex(13)]
        [UsedImplicitly]
        public short Morph { get; set; }

        [PacketIndex(14)]
        public bool Invisible { get; set; }

        [PacketIndex(15)]
        public byte FamilyLevel { get; set; }

        [PacketIndex(16)]
        public byte MorphUpgrade { get; set; }

        [PacketIndex(17)]
        public bool ArenaWinner { get; set; }

        #endregion
    }
}