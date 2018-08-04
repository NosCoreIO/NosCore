using JetBrains.Annotations;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("mvi")]
    public class MviPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public PocketType InventoryType { get; set; }

        [PacketIndex(1)]
        public short Slot { get; set; }

        [PacketIndex(2)]
        public byte Amount { get; set; }

        [PacketIndex(3)]
        public byte DestinationSlot { get; set; }

    }
}