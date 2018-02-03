using OpenNosCore.Core.Serializing;

namespace OpenNosCore.Packets.ClientPackets
{
    [PacketHeader("select")]
    public class SelectPacket : PacketDefinition
    {
        #region Properties
        [PacketIndex(0)]
        public byte Slot { get; set; }
        #endregion
    }
}