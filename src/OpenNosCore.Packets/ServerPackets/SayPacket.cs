using OpenNosCore.Core.Serializing;

namespace OpenNosCore.Packets
{
    [PacketHeader("say")]
    public class SayPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte VisualType { get; set; }

        [PacketIndex(1)]
        public long VisualId { get; set; }

        [PacketIndex(2)]
        public byte Type { get; set; }

        [PacketIndex(3)]
        public string Message { get; set; }

        [PacketIndex(4)]
        public byte MorphDesign { get; set; }

        [PacketIndex(5, IsOptional = true)]
        public byte MorphBonus { get; set; }

        #endregion
    }
}