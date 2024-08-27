using NosCore.Packets.Attributes;
using NosCore.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.Packets.Enumerations;

namespace NosCore.Data
{
    [PacketHeader("ski", Scope.InGame)]
    public class SkillPacket : PacketBase
    {
        [PacketListIndex(0, SpecialSeparator = " ")]
        public List<SubSkillPacket> Skills { get; set; } = new();
    }

    public class SubSkillPacket : PacketBase
    {
        [PacketIndex(0)]
        public int VNum { get; set; }
    }
}
