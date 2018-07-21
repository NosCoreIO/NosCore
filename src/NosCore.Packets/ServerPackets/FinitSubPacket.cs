using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Character;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("finit_subpacket")]
    public class FinitSubPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public long CharacterId { get; set; }

        [PacketIndex(1)]
        public CharacterRelationType RelationType { get; set; }

        [PacketIndex(2)]
        public bool IsOnline { get; set; }

        [PacketIndex(3, SerializeToEnd = true)]
        public string CharacterName { get; set; }
    }
}
