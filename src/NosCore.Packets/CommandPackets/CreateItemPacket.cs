using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$CreateItem", Authority = AuthorityType.GameMaster)]
    public class CreateItemPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public short VNum { get; set; }

        [PacketIndex(1)]
        public byte? Design { get; set; }

        [PacketIndex(2)]
        public byte? Upgrade { get; set; }

        public static string ReturnHelp()
        {
            return "$CreateItem ITEMVNUM DESIGN/RARE/AMOUNT/WINGS UPDATE";
        }
    }
}
