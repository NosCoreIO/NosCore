using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("drop")]
    public class DropPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public short VNum { get; set; }

        [PacketIndex(1)]
        public long VisualId { get; set; }

        [PacketIndex(2)]
        public short PositionX { get; set; }

        [PacketIndex(3)]
        public short PositionY { get; set; }

        [PacketIndex(4)]
        public short Amount { get; set; }

        [PacketIndex(5)]
        public byte Unknown { get; set; }

        [PacketIndex(6)]
        public long? OwnerId { get; set; }
    }
}