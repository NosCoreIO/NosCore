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

		[PacketIndex(7, IsOptional = true, RemoveSeparator = true)]
		public InCharacterSubPacket InCharacterSubPacket { get; set; }

		[PacketIndex(8, IsOptional = true, RemoveSeparator = true)]
		public InItemSubPacket InItemSubPacket { get; set; }

		[PacketIndex(9, IsOptional = true, RemoveSeparator = true)]
		public InNonPlayerSubPacket InNonPlayerSubPacket { get; set; }

        //1 -1 0 0 0 0 0 0 1 2 -1 - 7 0 0 0 0 19 0 0 0 10
        //0 0 3 793041 1 0 -1 Hamster^furieux 0 -1 0 0 0 0 0 0 0 0
        //{GroupId?} {(fairy ? 4 : 0)} {fairyElement} 0 {Morph} 0 {Morph} {GenerateEqRareUpgradeForPacket()} {FamilyId?} {Name} {(GetDignityIco() == 1 ? GetReputIco() : -GetDignityIco())} {isInvisible} {MorphUpgrade} {faction} {MorphUpgrade2} {Level} {FamilyLevel} {ArenaWinner} {Compliment} {Size} {HeroLevel}";

        #endregion
    }
}