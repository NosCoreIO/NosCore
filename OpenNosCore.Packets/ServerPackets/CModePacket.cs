using OpenNosCore.Core.Serializing;

namespace OpenNosCore.Packets
{
    [PacketHeader("c_mode")]
    public class CModePacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte VisualType { get; set; }

        [PacketIndex(1)]
        public long VisualId { get; set; }

        [PacketIndex(2)]
        public byte Morph { get; set; }

        [PacketIndex(3)]
        public byte MorphUpgrade { get; set; }

        [PacketIndex(4)]
        public byte MorphDesign { get; set; }

        [PacketIndex(5, IsOptional = true)]
        public byte MorphBonus { get; set; }

        #endregion
    }
}