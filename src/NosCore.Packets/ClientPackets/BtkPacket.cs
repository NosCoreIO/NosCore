using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("btk")]
    public class BtkPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public long CharacterId { get; set; }

        [PacketIndex(1, SerializeToEnd = true)]
        public string Message { get; set; }
    }
}
