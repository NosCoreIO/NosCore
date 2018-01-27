using OpenNosCore.Core.Serializing;

namespace OpenNosCore.Packets.ClientPackets
{
    [PacketHeader("Char_DEL")]
    public class CharacterDeletePacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte Slot { get; set; }

        [PacketIndex(1)]
        public string Password { get; set; }

    }
}
