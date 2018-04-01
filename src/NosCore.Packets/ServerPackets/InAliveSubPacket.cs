using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
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