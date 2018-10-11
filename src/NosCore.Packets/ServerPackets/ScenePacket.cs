using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("scene")]
    public class ScenePacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte SceneId { get; set; }
    }
}