using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("bldel")]
    public class BlDelPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public long CharacterId { get; set; }
    }
}
