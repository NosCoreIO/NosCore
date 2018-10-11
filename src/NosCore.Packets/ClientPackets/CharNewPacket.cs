using System.ComponentModel.DataAnnotations;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Character;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("Char_NEW", AnonymousAccess = true)]
    public class CharNewPacket : PacketDefinition
    {
        [PacketIndex(0)]
        [StringLength(15, MinimumLength = 3)]
        public string Name { get; set; }

        [PacketIndex(1)]
        [Range(0, 3)]
        public byte Slot { get; set; }

        [PacketIndex(2)]
        public GenderType Gender { get; set; }

        [PacketIndex(3)]
        public HairStyleType HairStyle { get; set; }

        [PacketIndex(4)]
        public HairColorType HairColor { get; set; }
    }
}