using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("blins")]
    public class BlInsPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public long CharacterId { get; set; }
    }
}
