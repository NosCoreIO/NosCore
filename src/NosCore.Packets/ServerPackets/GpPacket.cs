using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("gp")]
    public class GpPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public short SourceX { get; set; }

        [PacketIndex(1)]
        public short SourceY { get; set; }

        [PacketIndex(2)]
        public short MapId { get; set; }

        [PacketIndex(3)]
        public short Type { get; set; }

        [PacketIndex(4)]
        public int PortalId { get; set; }

        [PacketIndex(5)]
        public int IsDisabled { get; set; }
        #endregion
    }
}