using OpenNosCore.Core.Serializing;
using OpenNosCore.Domain.Character;

namespace OpenNosCore.Packets
{
    [PacketHeader("in_character_subpacket")]
    public class InCharacterSubPacket : PacketDefinition
    {
        #region Properties
        [PacketIndex(0)]
        public byte Authority { get; set; }

        [PacketIndex(1)]
        public GenderType Gender { get; set; }

        [PacketIndex(2)]
        public HairStyleType HairStyle { get; set; }

        [PacketIndex(3)]
        public HairColorType HairColor { get; set; }

        [PacketIndex(4)]
        public byte Class { get; set; }

        [PacketIndex(5)]
        public byte Equipment { get; set; }
        #endregion
    }
}