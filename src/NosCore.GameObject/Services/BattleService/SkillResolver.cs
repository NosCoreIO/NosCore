//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.GameObject.Ecs;
using System.Linq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.SkillService;

namespace NosCore.GameObject.Services.BattleService;

// Characters index their skills by CastId (matches client packets). Non-player entities
// pick from the catalog's NpcMonsterSkill table and fall back to their basic attack when the
// requested cast is not in their roster.
public sealed class SkillResolver : ISkillResolver
{
    private const short BasicAttackAnimation = 11;

    private readonly ISkillCatalog _skills;
    private readonly INpcCombatCatalog _catalog;
    private readonly ICardCatalog _cards;

    public SkillResolver(ISkillCatalog skills, INpcCombatCatalog catalog, ICardCatalog cards)
    {
        _skills = skills;
        _catalog = catalog;
        _cards = cards;
    }

    public SkillInfo? Resolve(IAliveEntity caster, long castId)
    {
        return caster switch
        {
            PlayerComponentBundle character => ResolveCharacter(character, castId),
            INonPlayableEntity npc => ResolveNpc(npc, castId),
            _ => null,
        };
    }

    public SkillInfo Describe(SkillDto skill) => BuildInfo(skill, null, skill.CastId, _catalog.GetSkillBCards(skill.SkillVNum));

    private SkillInfo? ResolveCharacter(PlayerComponentBundle character, long castId)
    {
        // The same skill family can have multiple upgrades; client sends the base CastId
        // and the server picks the highest-tier version the character owns.
        var match = FindLearned(character, castId);
        if (match?.Skill == null) return null;

        var upgrades = character.Skills.Values
            .Select(s => s.Skill)
            .OfType<SkillDto>()
            .Where(s => s.UpgradeSkill == match.Skill.SkillVNum && s.IsUpgrade())
            .OrderBy(s => s.SkillVNum)
            .ToList();

        return BuildInfo(match.Skill, upgrades.FirstOrDefault(s => s.Effect > 0), castId,
            WithUpgrades(match.Skill, upgrades));
    }

    // Every learned upgrade adds its effects to the skill's own, except that a card it causes takes the
    // place of the same card, or one of the same card group, that the skill already causes.
    private IReadOnlyList<BCardDto> WithUpgrades(SkillDto skill, IReadOnlyList<SkillDto> upgrades)
    {
        var bCards = _catalog.GetSkillBCards(skill.SkillVNum);
        if (upgrades.Count == 0)
        {
            return bCards;
        }

        var merged = new List<BCardDto>(bCards);
        foreach (var upgrade in upgrades)
        {
            foreach (var bCard in _catalog.GetSkillBCards(upgrade.SkillVNum))
            {
                merged.RemoveAll(earlier => Replaces(bCard, earlier));
                merged.Add(bCard);
            }
        }

        return merged;
    }

    private bool Replaces(BCardDto bCard, BCardDto earlier)
    {
        if (bCard.Effect() != BCardEffect.BuffChanceCausing || earlier.Effect() != BCardEffect.BuffChanceCausing)
        {
            return false;
        }

        return bCard.SecondData == earlier.SecondData
            || _cards.GetCard((short)bCard.SecondData) is { GroupId: not 0 } card
            && _cards.GetCard((short)earlier.SecondData)?.GroupId == card.GroupId;
    }

    // The packet guard spends cooldown and mana on this same skill, so both must pick it the same way.
    // Upgrades are type 2. A few active skills, every class's Capture among them, carry a
    // non-zero UpgradeSkill without being one, so they are only taken when no base skill is.
    public static CharacterSkill? FindLearned(PlayerComponentBundle character, long castId)
    {
        var owned = character.Skills?.Values
            .Where(s => s.Skill != null && !s.Skill.IsPassive() && s.Skill.CastId == castId)
            .ToList();
        if (owned == null) return null;
        return owned.FirstOrDefault(s => s.Skill.UpgradeSkill == 0)
            ?? owned.FirstOrDefault(s => s.Skill.SkillType != 2);
    }

    private SkillInfo? ResolveNpc(INonPlayableEntity npc, long castId)
    {
        var mob = npc.NpcMonster;
        if (mob == null) return null;

        var resolvedSkill = ResolveNpcSkill(mob, castId);
        return resolvedSkill != null
            ? BuildInfo(resolvedSkill, null, castId, _catalog.GetSkillBCards(resolvedSkill.SkillVNum))
            : BasicAttack(mob);
    }

    public SkillInfo? ResolveBasic(IAliveEntity caster) =>
        caster is INonPlayableEntity { NpcMonster: { } mob } ? BasicAttack(mob) : null;

    // A basic attack is no skill: its reach and cooldown are the monster's own, it carries no
    // cards, and BasicSkill is only the effect its hit plays.
    private static SkillInfo BasicAttack(NpcMonsterDto mob)
    {
        return new SkillInfo(
            SkillVnum: 0,
            CastId: 0,
            Cooldown: mob.BasicCooldown,
            AttackAnimation: BasicAttackAnimation,
            CastEffect: 0,
            Effect: mob.BasicSkill,
            Type: mob.AttackClass,
            HitType: TargetHitType.SingleTargetHit,
            Range: mob.BasicRange,
            TargetRange: mob.BasicArea,
            TargetType: 1,
            Element: mob.Element,
            Duration: 0,
            MpCost: 0,
            BCards: System.Array.Empty<BCardDto>());
    }

    private SkillDto? ResolveNpcSkill(NpcMonsterDto mob, long castId)
    {
        foreach (var sk in _catalog.GetSkills(mob.NpcMonsterVNum))
        {
            if (_skills.GetSkill(sk.SkillVNum) is { } s && (s.CastId == castId || castId == 0))
            {
                return s;
            }
        }
        return null;
    }

    private static SkillInfo BuildInfo(SkillDto main, SkillDto? upgrade, long castId, IReadOnlyList<BCardDto> bCards)
    {
        return new SkillInfo(
            SkillVnum: main.SkillVNum,
            CastId: castId,
            Cooldown: main.Cooldown,
            AttackAnimation: main.AttackAnimation,
            CastEffect: upgrade?.CastEffect ?? main.CastEffect,
            Effect: upgrade?.Effect ?? main.Effect,
            Type: main.Type,
            HitType: (TargetHitType)main.HitType,
            Range: main.Range,
            TargetRange: main.TargetRange,
            TargetType: main.TargetType,
            Element: main.Element,
            Duration: main.Duration,
            MpCost: main.MpCost,
            BCards: bCards,
            CellPattern: main.CellPattern,
            CastAnimation: main.CastAnimation,
            CastTime: main.CastTime,
            UsesSecondaryWeapon: main.UsesSecondaryWeapon,
            Kind: main.Kind);
    }
}
