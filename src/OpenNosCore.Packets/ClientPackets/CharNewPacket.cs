using OpenNosCore.Core.Serializing;
using OpenNosCore.Domain.Character;

namespace OpenNosCore.Packets.ClientPackets
{
    [PacketHeader("Char_NEW")]
    public class CharNewPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public string Name { get; set; }

        [PacketIndex(1)]
        public byte Slot { get; set; }

        [PacketIndex(2)]
        public GenderType Gender { get; set; }

        [PacketIndex(3)]
        public HairStyleType HairStyle { get; set; }

        [PacketIndex(4)]
        public HairColorType HairColor { get; set; }

    }
}
