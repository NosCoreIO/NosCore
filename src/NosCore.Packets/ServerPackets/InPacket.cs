using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
	[PacketHeader("in")]
	public class InPacket : PacketDefinition
	{
		#region Properties

		[PacketIndex(0)]
		public byte VisualType { get; set; }

		[PacketIndex(1, IsOptional = true)]
		public string Name { get; set; }

		[PacketIndex(2)]
		public string VNum { get; set; }

		[PacketIndex(3)]
		public long VisualId { get; set; }

        [PacketIndex(4)]
		public short PositionX { get; set; }

		[PacketIndex(5)]
		public short PositionY { get; set; }

		[PacketIndex(6, IsOptional = true)]
		public byte? Direction { get; set; }

		[PacketIndex(7, IsOptional = true)]
		public short? Amount { get; set; }

		[PacketIndex(8, IsOptional = true, RemoveSeparator = true)]
		public InCharacterSubPacket InCharacterSubPacket { get; set; }

		[PacketIndex(9, IsOptional = true, RemoveSeparator = true)]
		public InAliveSubPacket InAliveSubPacket { get; set; }

		[PacketIndex(10, IsOptional = true, RemoveSeparator = true)]
		public InItemSubPacket InItemSubPacket { get; set; }

		[PacketIndex(11, IsOptional = true, RemoveSeparator = true)]
		public InNonPlayerSubPacket InNonPlayerSubPacket { get; set; }

		[PacketIndex(12, IsOptional = true, RemoveSeparator = true)]
		public InOwnableSubPacket InOwnableSubPacket { get; set; }

		[PacketIndex(13)]
        public bool IsSitting { get; set; }

        //-1 4 3 0 0 0 7 86 86 2340 ~Luna~(Membre) -2 0 5 0 0 88 10 0 0 10 1
        //{GroupId?} {(fairy ? 4 : 0)} {fairyElement} 0 {Morph} 0 {Morph} {GenerateEqRareUpgradeForPacket()} {FamilyId?} {Name} {(GetDignityIco() == 1 ? GetReputIco() : -GetDignityIco())} {isInvisible} {MorphUpgrade} {faction} {MorphUpgrade2} {Level} {FamilyLevel} {ArenaWinner} {Compliment} {Size} {HeroLevel}";

        #endregion
    }
}