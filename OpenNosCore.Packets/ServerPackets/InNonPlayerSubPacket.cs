using OpenNosCore.Core.Serializing;

namespace OpenNosCore.Packets
{
    [PacketHeader("in_non_player_subpacket")]
    public class InNonPlayerSubPacket : PacketDefinition
    {
        #region Properties
        [PacketIndex(0)]
        public short Dialog { get; set; }

        [PacketIndex(1)]
        public byte Unknown { get; set; }
        
        #endregion
    }
}