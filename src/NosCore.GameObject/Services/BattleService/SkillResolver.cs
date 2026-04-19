//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService;

// Characters index their skills by CastId (matches client packets). Non-player entities
// pick from the catalog's NpcMonsterSkill table and fall back to BasicSkill when the
// requested cast is not in their roster — this is also what "monster attacks something"
// ends up calling with castId == 0.
public sealed class SkillResolver : ISkillResolver
{
    private readonly IReadOnlyDictionary<short, SkillDto> _skillsByVnum;
    private readonly INpcCombatCatalog _catalog;

    // Bootstrap registration path: gets the pre-loaded List<SkillDto> from the DI
    // container and indexes it here. Tests use the dictionary overload.
    public SkillResolver(List<SkillDto> skills, INpcCombatCatalog catalog)
        : this(skills.ToDictionary(s => s.SkillVNum, s => s), catalog)
    {
    }

    public SkillResolver(IReadOnlyDictionary<short, SkillDto> skillsByVnum, INpcCombatCatalog catalog)
    {
        _skillsByVnum = skillsByVnum;
        _catalog = catalog;
    }

    public SkillInfo? Resolve(IAliveEntity caster, long castId)
    {
        return caster switch
        {
            ICharacterEntity character => ResolveCharacter(character, castId),
            INonPlayableEntity npc => ResolveNpc(npc, castId),
            _ => null,
        };
    }

    private SkillInfo? ResolveCharacter(ICharacterEntity character, long castId)
    {
        // The same skill family can have multiple upgrades; client sends the base CastId
        // and the server picks the highest-tier version the character owns.
        var match = character.Skills.Values
            .Where(s => s.Skill != null && s.Skill.CastId == castId && s.Skill.UpgradeSkill == 0)
            .FirstOrDefault();
        if (match?.Skill == null) return null;

        var upgrade = character.Skills.Values
            .Select(s => s.Skill)
            .Where(s => s != null && s!.UpgradeSkill == match.Skill!.SkillVNum && s.Effect > 0 && s.SkillType == 2)
            .OrderBy(s => s!.SkillVNum)
            .FirstOrDefault();

        return BuildInfo(match.Skill!, upgrade, castId);
    }

    private SkillInfo? ResolveNpc(INonPlayableEntity npc, long castId)
    {
        var mob = npc.NpcMonster;
        if (mob == null) return null;

        var resolvedSkill = ResolveNpcSkill(mob, castId);
        if (resolvedSkill == null) return null;

        return BuildInfo(resolvedSkill, null, castId);
    }

    private SkillDto? ResolveNpcSkill(NpcMonsterDto mob, long castId)
    {
        // If the monster knows this skill vnum, use it; otherwise fall back to the
        // monster's default attack. The fall-through covers the extremely common
        // case where the AI just calls Resolve(mob, 0) to mean "basic attack".
        foreach (var sk in _catalog.GetSkills(mob.NpcMonsterVNum))
        {
            if (_skillsByVnum.TryGetValue(sk.SkillVNum, out var s) && (s.CastId == castId || castId == 0))
            {
                return s;
            }
        }
        if (mob.BasicSkill > 0 && _skillsByVnum.TryGetValue(mob.BasicSkill, out var basic))
        {
            return basic;
        }
        return null;
    }

    private SkillInfo BuildInfo(SkillDto main, SkillDto? upgrade, long castId)
    {
        return new SkillInfo(
            SkillVnum: main.SkillVNum,
            CastId: castId,
            Cooldown: main.Cooldown,
            AttackAnimation: main.AttackAnimation,
            CastEffect: upgrade?.CastEffect ?? main.CastEffect,
            Effect: main.Effect,
            Type: main.Type,
            HitType: (TargetHitType)main.HitType,
            Range: main.Range,
            TargetRange: main.TargetRange,
            TargetType: main.TargetType,
            Element: main.Element,
            Duration: main.Duration,
            MpCost: main.MpCost,
            BCards: _catalog.GetSkillBCards(main.SkillVNum));
    }
}
