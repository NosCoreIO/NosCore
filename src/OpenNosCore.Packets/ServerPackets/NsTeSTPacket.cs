using OpenNosCore.Core.Serializing;
using OpenNosCore.Packets.ServerPackets;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenNosCore.Packets
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
