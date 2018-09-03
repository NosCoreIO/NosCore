using System.Collections.Generic;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("finfo")]
    public class FinfoPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public List<FinfoSubPackets> FriendList { get; set; }
    }
}
