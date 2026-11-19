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
// pick from the catalog's NpcMonsterSkill table and fall back to BasicSkill when the
// requested cast is not in their roster — this is also what "monster attacks something"
// ends up calling with castId == 0.
public sealed class SkillResolver : ISkillResolver
{
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

    public SkillInfo? ResolveBasic(IAliveEntity caster)
    {
        if (caster is not INonPlayableEntity { NpcMonster: { } mob })
        {
            return null;
        }

        return mob.BasicSkill > 0 && _skills.GetSkill(mob.BasicSkill) is { } basic
            ? BuildInfo(basic, null, 0, _catalog.GetSkillBCards(basic.SkillVNum))
            : BasicAttack(mob);
    }

    private static SkillInfo BasicAttack(NpcMonsterDto mob)
    {
        // No skill in the catalog and no BasicSkill vnum — but the monster still
        // needs to hit. The overwhelming majority of low-level mobs fall here
        // (melee wolf, rat, etc.) so we synthesise a minimal basic-attack SkillInfo
        // from the monster's raw stats. Type/AttackClass follow the monster's attack class
        // (0 melee, 1 ranged, 2 magic), Range/Cooldown come from the
        // NpcMonsterDto itself, and Damage comes from the min/max hit range via
        // CombatStats — not via skill BCards.
        return new SkillInfo(
            SkillVnum: 0,
            CastId: 0,
            Cooldown: mob.BasicCooldown,
            AttackAnimation: 0,
            CastEffect: 0,
            Effect: 0,
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
        // If the monster knows this skill vnum, use it; otherwise fall back to the
        // monster's default attack. The fall-through covers the extremely common
        // case where the AI just calls Resolve(mob, 0) to mean "basic attack".
        foreach (var sk in _catalog.GetSkills(mob.NpcMonsterVNum))
        {
            if (_skills.GetSkill(sk.SkillVNum) is { } s && (s.CastId == castId || castId == 0))
            {
                return s;
            }
        }
        if (mob.BasicSkill > 0 && _skills.GetSkill(mob.BasicSkill) is { } basic)
        {
            return basic;
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
