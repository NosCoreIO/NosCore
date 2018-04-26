using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("spk")]
    public class SpkPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte PacketType { get; set; }

        [PacketIndex(1)]
        public long VisualEntityId { get; set; }

        [PacketIndex(2)]
        public int Type { get; set; }

        [PacketIndex(3)]
        public string VisualEntityName { get; set; }

        [PacketIndex(4)]
        public string Message { get; set; }

        #endregion
    }
}
