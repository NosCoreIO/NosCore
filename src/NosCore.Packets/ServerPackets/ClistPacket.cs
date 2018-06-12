using System.Collections.Generic;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Character;

namespace NosCore.Packets.ServerPackets
{
	[PacketHeader("clist")]
	public class ClistPacket : PacketDefinition
	{
		#region Properties

		[PacketIndex(0)]
		public byte Slot { get; set; }

		[PacketIndex(1)]
		public string Name { get; set; }

		[PacketIndex(2)]
		public byte Unknown { get; set; }

		[PacketIndex(3)]
		public byte Gender { get; set; }

		[PacketIndex(4)]
		public byte HairStyle { get; set; }

		[PacketIndex(5)]
		public byte HairColor { get; set; }

		[PacketIndex(6)]
		public byte Unknown1 { get; set; }

		[PacketIndex(7)]
		public CharacterClassType Class { get; set; }

		[PacketIndex(8)]
		public byte Level { get; set; }

		[PacketIndex(9)]
		public byte HeroLevel { get; set; }

		[PacketIndex(10)]
		public List<short?> Equipments { get; set; }

		[PacketIndex(11)]
		public byte JobLevel { get; set; }

		[PacketIndex(12)]
		public byte QuestCompletion { get; set; }

		[PacketIndex(13)]
		public byte QuestPart { get; set; }

		[PacketIndex(14)]
		public List<short?> Pets { get; set; }

		[PacketIndex(15)]
		public int Design { get; set; }

		[PacketIndex(16)]
		public byte Unknown3 { get; set; }

		#endregion
	}
}