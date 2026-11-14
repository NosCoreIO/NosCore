//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.EquipmentService;

namespace NosCore.GameObject.Services.BattleService;

public sealed class SkillCastService(IClock clock, IEquipmentStatsService equipmentStatsService, IBuffService buffService)
    : ISkillCastService
{
    public CharacterSkill? FindLearned(PlayerComponentBundle character, long castId) =>
        SkillResolver.FindLearned(character, castId);

    public SkillCastRefusal Check(PlayerComponentBundle character, CharacterSkill skill, out int mpCost)
    {
        mpCost = 0;
        var now = clock.GetCurrentInstant();
        if (skill.Skill == null || skill.LastUse is { } lastUse && lastUse.Plus(Cooldown(character, skill.Skill.Cooldown)) >= now)
        {
            return SkillCastRefusal.OnCooldown;
        }

        var equipment = equipmentStatsService.Resolve(character);
        var decrease = Math.Clamp(equipment.MpConsumptionDecrease, 0, 100);
        var percent = Percent(character, equipment.BCards, BCardEffect.CastingManaForSkillsIncreased, BCardEffect.CastingManaForSkillsDecreased);
        mpCost = skill.Skill.MpCost * (100 - decrease) / 100 * Math.Max(0, 100 + percent) / 100;
        return character.Mp < mpCost ? SkillCastRefusal.NotEnoughMp : SkillCastRefusal.None;
    }

    public void Spend(PlayerComponentBundle character, CharacterSkill skill, int mpCost)
    {
        skill.LastUse = clock.GetCurrentInstant();
        character.Mp = Math.Max(0, character.Mp - mpCost);
    }

    public Duration Cooldown(PlayerComponentBundle character, short cooldown)
    {
        var percent = Percent(character, equipmentStatsService.Resolve(character).BCards,
            BCardEffect.MoraleSkillCooldownIncreased, BCardEffect.MoraleSkillCooldownDecreased);
        return Duration.FromMilliseconds(cooldown * 100L * Math.Max(0, 100 + percent) / 100);
    }

    private int Percent(PlayerComponentBundle character, IEnumerable<BCardDto>? gear, BCardEffect increase, BCardEffect decrease) =>
        buffService.GetActiveBuffs(character).SelectMany(b => b.BCards)
            .Concat(gear ?? [])
            .Sum(bCard => bCard.Effect() == increase ? BattleStatsProvider.ScaleByLevel(bCard, character.Level)
                : bCard.Effect() == decrease ? -BattleStatsProvider.ScaleByLevel(bCard, character.Level)
                : 0);
}
