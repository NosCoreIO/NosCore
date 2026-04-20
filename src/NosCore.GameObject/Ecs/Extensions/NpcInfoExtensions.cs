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
    // Builds the e_info response for a req_info 5 (NPC) or req_info 6 (monster/mate).
    // OpenNos's NpcMonster.GenerateEInfo AND Mate.GenerateEInfo both emit:
    //   `e_info 10 <vnum> <level> <element> <attackClass> <elementRate> <atkUp>
    //    <dmgMin> <dmgMax> <concentrate> <critChance> <critRate> <defUp> <closeDef>
    //    <defDodge> <distDef> <distDodge> <magicDef> <fire> <water> <light> <dark>
    //    <maxHp> <maxMp> -1 <name>`
    // — the leading 10 is the format discriminator and the trailing -1 is a constant
    // the client expects before the name field. Without either, the client can't align
    // fields and falls back to defaults (Level=0, HP=100/100) in the target info card.
    public static EInfoNpcMonsterPacket GenerateNpcInfo(this NpcMonsterDto npc, RegionType language)
    {
        var name = npc.Name is null ? string.Empty : npc.Name[language];
        return new EInfoNpcMonsterPacket
        {
            SubType = 10,
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
            Unknown = -1,
            Name = (name ?? string.Empty).Replace(' ', '^'),
        };
    }

    // Mate.GenerateEInfo reuses the monster's stat block with the mate's own level/name.
    // Same subtype=10 as NpcMonster.GenerateEInfo — OpenNos reuses the discriminator.
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
