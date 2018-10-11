using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("finfo_sub_packets")]
    public class FinfoSubPackets : PacketDefinition
    {
        [PacketIndex(0)]
        public long CharacterId { get; set; }

        [PacketIndex(1)]
        public bool IsConnected { get; set; }
    }
}
