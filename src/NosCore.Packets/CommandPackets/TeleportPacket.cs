using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$Teleport", Authority = AuthorityType.GameMaster)]
    public class TeleportPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public string TeleportArgument { get; set; }

        [PacketIndex(1)]
        public short MapX { get; set; }

        [PacketIndex(2)]
        public short MapY { get; set; }
    }
}
