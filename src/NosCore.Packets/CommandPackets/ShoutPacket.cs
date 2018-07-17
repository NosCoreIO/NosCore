using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$Shout", Authority = AuthorityType.GameMaster)]
    public class ShoutPacket : PacketDefinition
    {
        [PacketIndex(0, SerializeToEnd = true)]
        public string Message { get; set; }
    }
}
