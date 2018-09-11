using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader(";")]
    public class GroupTalkPacket : PacketDefinition
    {
         [PacketIndex(0, SerializeToEnd = true)]
         public string Message { get; set; }
    }
}
