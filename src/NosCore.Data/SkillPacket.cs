using NosCore.Packets.Attributes;
using NosCore.Packets;
using System.Collections.Generic;
using NosCore.Packets.Enumerations;

//todo move to NosCore.Packets
namespace NosCore.Data
{
    [PacketHeader("ski", Scope.InGame)]
    public class SkillPacket : PacketBase
    {
        [PacketIndex(0)]
        public int MainSkill { get; set; }

        [PacketIndex(1)]
        public int SecondarySkill { get; set; }

        [PacketListIndex(2, SpecialSeparator = " ")]
        public List<SubSkillPacket> Skills { get; set; } = new();
    }

    public class SubSkillPacket : PacketBase
    {
        [PacketIndex(0)]
        public int VNum { get; set; }
    }
}
