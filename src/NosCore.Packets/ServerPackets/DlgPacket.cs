using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("dlg")]
    public class DlgPacket : PacketDefinition
    {
        [PacketIndex(0, IsReturnPacket = true)]
        public PacketDefinition YesPacket { get; set; }

        [PacketIndex(1, IsReturnPacket = true)]
        public PacketDefinition NoPacket { get; set; }

        [PacketIndex(2, SerializeToEnd = true)]
        public string Question { get; set; }
    }
}