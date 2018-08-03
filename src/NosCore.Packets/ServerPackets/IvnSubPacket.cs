using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("ivn_subpacket")]
    public class IvnSubPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public short Slot { get; set; }
        [PacketIndex(1)]
        public short VNum { get; set; }
        [PacketIndex(2)]
        public short Rare { get; set; }
        [PacketIndex(3)]
        public byte Upgrade { get; set; }
        [PacketIndex(4)]
        public byte SecondUpgrade { get; set; }
    }
}