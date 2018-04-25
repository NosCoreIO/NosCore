using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("/")]
    public class WhisperPacket : PacketDefinition
    {
        [PacketIndex(0, SerializeToEnd = true)]
        public string Message { get; set; }
    }
}
