using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("msg")]
    public class MsgPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte Type { get; set; } //Todo: Find what it exactly does to make an enum

        [PacketIndex(1, SerializeToEnd = true)]
        public string Message { get; set; }
    }
}
