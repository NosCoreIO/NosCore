using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("fins")]
    public class FinsPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public FinsPacketType Type { get; set; }

        [PacketIndex(1)]
        public long CharacterId { get; set; }
    }
}
