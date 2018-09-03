using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("talk")]
    public class TalkPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public long CharacterId { get; set; }

        [PacketIndex(1, SerializeToEnd = true)]
        public string Message { get; set; }
    }
}
