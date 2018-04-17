using NosCore.Core.Serializing;


namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("eff")]
    public class EffectPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte EffectType { get; set; }

        [PacketIndex(1)]
        public long VisualEntityId { get; set; }

        [PacketIndex(2)]
        public int Id { get; set; }

        #endregion
    }
}
