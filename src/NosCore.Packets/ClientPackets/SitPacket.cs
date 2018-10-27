using NosCore.Core.Serializing;
using System.Collections.Generic;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("rest")]
    public class SitPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte Amount { get; set; }

        [PacketIndex(1, RemoveSeparator = true)]
        public List<SitSubPacket> Users { get; set; }

        #endregion
    }
}
