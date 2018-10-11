using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("eff")]
    public class EffectPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public VisualType EffectType { get; set; }

        [PacketIndex(1)]
        public long VisualEntityId { get; set; }

        [PacketIndex(2)]
        public int Id { get; set; }

        #endregion
    }
}