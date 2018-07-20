using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("blinit_subpacket")]
    public class BlinitSubPacket : PacketDefinition
    {
        [PacketIndex(0, SpecialSeparator = "|")]
        public long RelatedCharacterId { get; set; }

        [PacketIndex(1, SpecialSeparator = "|")]
        public string CharacterName { get; set; }
    }
}
