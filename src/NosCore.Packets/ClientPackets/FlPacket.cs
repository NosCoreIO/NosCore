using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("$fl")]
    public class FlPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public string CharacterName { get; set; }
    }
}
