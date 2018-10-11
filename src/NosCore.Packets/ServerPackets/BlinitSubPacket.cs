using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("blinit_subpacket")]
    public class BlinitSubPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public long RelatedCharacterId { get; set; }

        [PacketIndex(1)]
        public string CharacterName { get; set; }
    }
}
