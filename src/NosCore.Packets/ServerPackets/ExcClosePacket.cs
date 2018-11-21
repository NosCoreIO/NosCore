using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("exc_close")]
    public class ExcClosePacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte Type { get; set; }
    }
}
