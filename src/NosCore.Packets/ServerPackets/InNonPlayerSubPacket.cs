using JetBrains.Annotations;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
	[PacketHeader("in_non_player_subpacket")]
	public class InNonPlayerSubPacket : PacketDefinition
	{
		[PacketIndex(1, RemoveSeparator = true)]
		public InAliveSubPacket InAliveSubPacket { get; set; }

		[PacketIndex(2)]
		public byte Dialog { get; set; }

		[PacketIndex(3)]
		public byte Faction { get; set; }

		[PacketIndex(4)]
		[UsedImplicitly]
        public short Effect { get; set; }

		[PacketIndex(5)]
		[UsedImplicitly]
        public long? Owner { get; set; }

		[PacketIndex(6)]
		[UsedImplicitly]
        public short Unknow { get; set; }

        [PacketIndex(7)]
        [UsedImplicitly]
        public bool IsSitting { get; set; }

		[PacketIndex(8)]
		[UsedImplicitly]
        public short? Morph { get; set; }

		[PacketIndex(9)]
		public string Name { get; set; }

        [PacketIndex(10)]
        [UsedImplicitly]
        public byte Unknow2 { get; set; }

		[PacketIndex(11)]
		[UsedImplicitly]
        public short? Unknow3 { get; set; }

		[PacketIndex(12)]
		[UsedImplicitly]
        public byte Unknow4 { get; set; }

		[PacketIndex(13)]
		[UsedImplicitly]
        public short Skill1 { get; set; }

		[PacketIndex(14)]
		[UsedImplicitly]
        public short Skill2 { get; set; }

		[PacketIndex(15)]
		[UsedImplicitly]
        public short Skill3 { get; set; }

		[PacketIndex(16)]
		[UsedImplicitly]
        public short SkillRank1 { get; set; }

		[PacketIndex(17)]
		[UsedImplicitly]
        public short SkillRank2 { get; set; }

		[PacketIndex(18)]
		[UsedImplicitly]
        public short SkillRank3 { get; set; }

		[PacketIndex(19)]
		[UsedImplicitly]
        public byte Unknow5 { get; set; }
    }
}