using System.Collections.Generic;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("inv")]
    public class InvPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public PocketType Type { get; set; }

        [PacketIndex(1)]
        public List<IvnSubPacket> IvnSubPackets { get; set; }
    }
}
