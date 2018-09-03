using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("$bl")]
    public class BlPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public string CharacterName { get; set; }
    }
}
