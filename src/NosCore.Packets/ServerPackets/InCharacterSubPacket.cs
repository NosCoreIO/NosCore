using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("in_character_subpacket")]
    public class InCharacterSubPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte Authority { get; set; }

        [PacketIndex(1)]
        public byte Gender { get; set; }

        [PacketIndex(2)]
        public byte HairStyle { get; set; }

        [PacketIndex(3)]
        public byte HairColor { get; set; }

        [PacketIndex(4)]
        public byte Class { get; set; }

        [PacketIndex(5)]
        public InEquipmentSubPacket Equipment { get; set; }

        [PacketIndex(6, RemoveSeparator = true)]
        public InAliveSubPacket InAliveSubPacket { get; set; }

        [PacketIndex(7)]
        public bool IsSitting { get; set; }

        [PacketIndex(8)]
        public int? GroupId { get; set; }

	    [PacketIndex(9)]
	    public byte Fairy { get; set; }

	    [PacketIndex(10)]
	    public byte FairyElement { get; set; }

	    [PacketIndex(11)]
	    public byte Unknown { get; set; }

	    [PacketIndex(12)]
	    public byte Morph { get; set; }

	    [PacketIndex(13)]
	    public byte WeaponUpgrade { get; set; }

	    [PacketIndex(14)]
	    public short WeaponRare { get; set; }

	    [PacketIndex(15)]
	    public byte ArmorUpgrade { get; set; }

	    [PacketIndex(16)]
	    public short ArmorRare { get; set; }

	    [PacketIndex(17)]
	    public long FamilyId { get; set; }

	    [PacketIndex(18)]
	    public string FamilyName { get; set; }

	    [PacketIndex(19)]
	    public short ReputIco { get; set; }

	    [PacketIndex(20)]
	    public bool Invisible { get; set; }

	    [PacketIndex(21)]
	    public byte MorphUpgrade { get; set; }

	    [PacketIndex(22)]
	    public byte Faction { get; set; }

	    [PacketIndex(23)]
	    public byte MorphUpgrade2 { get; set; }

	    [PacketIndex(24)]
	    public byte Level { get; set; }

	    [PacketIndex(25)]
	    public byte FamilyLevel { get; set; }

	    [PacketIndex(26)]
	    public bool ArenaWinner { get; set; }

	    [PacketIndex(27)]
	    public short Compliment { get; set; }

	    [PacketIndex(28)]
	    public byte Size { get; set; }

	    [PacketIndex(29)]
	    public byte HeroLevel { get; set; }
        #endregion
    }
}