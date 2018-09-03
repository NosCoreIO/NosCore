using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("fdel")]
    public class FdelPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public long CharacterId { get; set; }
    }
}
