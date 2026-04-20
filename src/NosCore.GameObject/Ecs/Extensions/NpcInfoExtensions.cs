//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Extensions;

public static class NpcInfoExtensions
{
    // Builds the e_info response for a req_info 5 (NPC) or req_info 6 (mate). OpenNos's
    // NpcMonster.GenerateEInfo / Mate.GenerateEInfo emit the same 26-field subtype-10 layout
    // — EInfoNpcMonsterPacket in NosCore.Packets mirrors that shape verbatim. The name is
    // space-collapsed to '^' because the client tokenises on space.
    public static EInfoNpcMonsterPacket GenerateNpcInfo(this NpcMonsterDto npc, RegionType language)
    {
        var name = npc.Name is null ? string.Empty : npc.Name[language];
        return new EInfoNpcMonsterPacket
        {
            NpcMonsterVNum = npc.NpcMonsterVNum,
            Level = npc.Level,
            Element = npc.Element,
            AttackClass = npc.AttackClass,
            ElementRate = npc.ElementRate,
            AttackUpgrade = npc.AttackUpgrade,
            DamageMinimum = npc.DamageMinimum,
            DamageMaximum = npc.DamageMaximum,
            Concentrate = npc.Concentrate,
            CriticalChance = npc.CriticalChance,
            CriticalRate = npc.CriticalRate,
            DefenceUpgrade = npc.DefenceUpgrade,
            CloseDefence = npc.CloseDefence,
            DefenceDodge = npc.DefenceDodge,
            DistanceDefence = npc.DistanceDefence,
            DistanceDefenceDodge = npc.DistanceDefenceDodge,
            MagicDefence = npc.MagicDefence,
            FireResistance = npc.FireResistance,
            WaterResistance = npc.WaterResistance,
            LightResistance = npc.LightResistance,
            DarkResistance = npc.DarkResistance,
            MaxHp = npc.MaxHp,
            MaxMp = npc.MaxMp,
            Name = (name ?? string.Empty).Replace(' ', '^'),
        };
    }

    // Mate.GenerateEInfo reuses the monster's stat block with the mate's own level/name,
    // so we start from the underlying NpcMonster shape and patch in mate-specific fields.
    public static EInfoNpcMonsterPacket GenerateMateInfo(this MateDto mate, NpcMonsterDto npcMonster)
    {
        var packet = npcMonster.GenerateNpcInfo(RegionType.EN);
        packet.Level = mate.Level;
        packet.MaxHp = npcMonster.MaxHp;
        packet.MaxMp = npcMonster.MaxMp;
        packet.Name = (mate.Name ?? string.Empty).Replace(' ', '^');
        return packet;
    }
}
