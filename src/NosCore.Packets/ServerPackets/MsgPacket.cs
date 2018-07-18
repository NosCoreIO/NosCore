using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("msg")]
    public class MsgPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public MessageType Type { get; set; }

        [PacketIndex(1, SerializeToEnd = true)]
        public string Message { get; set; }
    }
}
