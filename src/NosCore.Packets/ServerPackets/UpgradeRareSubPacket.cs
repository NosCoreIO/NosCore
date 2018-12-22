using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("upgrade_rare_sub_packet")]
    public class UpgradeRareSubPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte Upgrade { get; set; }

        [PacketIndex(1)]
        public byte Rare { get; set; }
    }
}
