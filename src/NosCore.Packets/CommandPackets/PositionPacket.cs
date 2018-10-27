using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Account;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$Position", Authority = AuthorityType.GameMaster)]
    public class PositionPacket : PacketDefinition
    {
        public static string ReturnHelp()
        {
            return "$Position";
        }
    }
}
