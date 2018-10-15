using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("pulse")]
    public class PulsePacket : PacketDefinition
    {
        [PacketIndex(0)]
        public int Tick { get; set; }
    }
}