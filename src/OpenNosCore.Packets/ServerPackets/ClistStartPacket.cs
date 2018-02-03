using OpenNosCore.Core.Serializing;

namespace OpenNosCore.Packets
{
    [PacketHeader("clist_start")]
    public class ClistStartPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte Type { get; set; }

        #endregion
    }
}