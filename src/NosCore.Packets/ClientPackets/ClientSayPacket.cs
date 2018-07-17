using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("say")]
    public class ClientSayPacket : PacketDefinition
    {
        [PacketIndex(0, SerializeToEnd = true)]
        public string Message { get; set; }
    }
}
