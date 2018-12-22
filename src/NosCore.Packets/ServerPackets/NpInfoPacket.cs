using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("npinfo")]
    public class NpInfoPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte Page { get; set; }
    }
}
