using OpenNosCore.Core.Serializing;

namespace OpenNosCore.Packets
{
    [PacketHeader("in_alive_subpacket")]
    public class InAliveSubPacket : PacketDefinition
    {
        #region Properties
        [PacketIndex(0)]
        public int HP { get; set; }

        [PacketIndex(1)]
        public int MP { get; set; }
        
        #endregion
    }
}