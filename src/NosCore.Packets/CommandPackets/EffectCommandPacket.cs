using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Account;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$Effect", Authority = AuthorityType.GameMaster)]
    public class EffectCommandPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public int EffectId { get; set; }

        public static string ReturnHelp()
        {
            return "$Effect EFFECTID";
        }
    }
}
