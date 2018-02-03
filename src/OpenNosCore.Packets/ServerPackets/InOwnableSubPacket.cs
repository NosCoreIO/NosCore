using OpenNosCore.Core.Serializing;

namespace OpenNosCore.Packets
{
    [PacketHeader("in_ownable_subpacket")]
    public class InOwnableSubPacket : PacketDefinition
    {
        #region Properties
        [PacketIndex(0)]
        public short? Unknown { get; set; }

        [PacketIndex(1)]
        public long Owner { get; set; }
        
        #endregion
    }
}