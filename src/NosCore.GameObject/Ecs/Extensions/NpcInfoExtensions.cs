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
    public static EInfoNpcMonsterPacket GenerateNpcInfo(this NpcMonsterDto npc, RegionType language)
    {
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
            // The serializer escapes a string field against the separator that follows it, and
            // nothing follows this one, so the spaces have to go before it is handed over.
            Name = npc.Name[language].Replace(' ', '^'),
        };
    }

    public static EInfoNpcMonsterPacket GenerateMateInfo(this MateDto mate, NpcMonsterDto npcMonster)
    {
        var packet = npcMonster.GenerateNpcInfo(RegionType.EN);
        packet.Level = mate.Level;
        packet.MaxHp = npcMonster.MaxHp;
        packet.MaxMp = npcMonster.MaxMp;
        return packet;
    }
}
