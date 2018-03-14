using NosCore.Core.Serializing;
using NosCore.Packets.ServerPackets;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Packets
{
    [PacketHeader("NsTeST")]
    public class NSTestPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string AccountName { get; set; }

        [PacketIndex(1)]
        public int SessionId { get; set; }

        [PacketIndex(2)]
        public List<NsTeSTSubPacket> SubPacket { get; set; }
        
        #endregion
    }
}
