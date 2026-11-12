//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.GameObject.Ecs;
using System.Linq;
using NodaTime;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.EquipmentService;

namespace NosCore.GameObject.Services.BattleService;

public sealed class SkillCastService(IClock clock, IEquipmentStatsService equipmentStatsService) : ISkillCastService
{
    public CharacterSkill? FindLearned(PlayerComponentBundle character, long castId) =>
        SkillResolver.FindLearned(character, castId);

    public SkillCastRefusal Check(PlayerComponentBundle character, CharacterSkill skill, out int mpCost)
    {
        mpCost = 0;
        if (skill.Skill == null || !skill.CanBeUsed(clock.GetCurrentInstant()))
        {
            return SkillCastRefusal.OnCooldown;
        }

        var decrease = Math.Clamp(equipmentStatsService.Resolve(character).MpConsumptionDecrease, 0, 100);
        mpCost = skill.Skill.MpCost * (100 - decrease) / 100;
        return character.Mp < mpCost ? SkillCastRefusal.NotEnoughMp : SkillCastRefusal.None;
    }

    public void Spend(PlayerComponentBundle character, CharacterSkill skill, int mpCost)
    {
        skill.LastUse = clock.GetCurrentInstant();
        character.Mp = Math.Max(0, character.Mp - mpCost);
    }
}
