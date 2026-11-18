//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.EquipmentService;

namespace NosCore.GameObject.Services.BattleService;

// Gear, buffs and a monster's own cards that give a chance, on a landed hit, to put a card on the
// opponent or on oneself: FirstData is the chance, SecondData the card. A weapon card only rolls
// on a hit with that weapon.
public sealed class OnHitBuffService(IBuffService buffService, IEquipmentStatsService equipmentStatsService,
        INpcCombatCatalog npcCombatCatalog)
    : IOnHitBuffService
{
    public async Task TriggerAsync(IAliveEntity attacker, IAliveEntity defender, bool defenderKilled,
        IReadOnlyList<BCardDto> skillBCards, bool usesSecondaryWeapon)
    {
        var attackerCards = Cards(attacker);
        var defenderCards = Cards(defender);
        if (!defenderKilled)
        {
            await InflictMonsterCardsAsync(attacker, defender, skillBCards);
            await InflictAsync(defender, attacker, attackerCards, BCardEffect.ChanceCausingXBuffOnEnemyOnAttack,
                usesSecondaryWeapon
                    ? BCardEffect.SpecialEffects2SecondaryWeaponCausingChance
                    : BCardEffect.SpecialEffects2MainWeaponCausingChance);
            await InflictAsync(defender, defender, defenderCards, BCardEffect.ChanceReceivingXBuffOnDefence);
        }

        await InflictAsync(attacker, attacker, attackerCards, BCardEffect.ChanceReceivingXBuffOnAttack);
        await InflictAsync(attacker, defender, defenderCards, BCardEffect.ChanceCausingXBuffOnEnemyOnDefence,
            BCardEffect.SecondSpCardCastBuffOnAttacker, BCardEffect.SecondSpCardChanceCausingXBuffOnAttacker);
    }

    // A card the skill used already rolls for is not rolled a second time.
    private Task InflictMonsterCardsAsync(IAliveEntity attacker, IAliveEntity defender, IReadOnlyList<BCardDto> skillBCards)
    {
        if (attacker is MateComponentBundle || attacker is not INonPlayableEntity { NpcMonster: { } monster } || !defender.IsAlive)
        {
            return Task.CompletedTask;
        }

        var cards = npcCombatCatalog.GetNpcBCards(monster.NpcMonsterVNum)
            .Where(c => c.Slot == 0 && c.Recipient == BCardRecipient.Enemy && c.Effect() == BCardEffect.BuffChanceCausing
                && !skillBCards.Any(s => s.Effect() == BCardEffect.BuffChanceCausing && s.SecondData == c.SecondData))
            .ToList();
        return cards.Count == 0 ? Task.CompletedTask : buffService.InflictCardsAsync(defender, attacker, cards);
    }

    private List<BCardDto> Cards(IAliveEntity entity) =>
        buffService.GetActiveBuffs(entity).SelectMany(b => b.BCards)
            .Concat(equipmentStatsService.Resolve(entity).BCards ?? [])
            .ToList();

    private Task InflictAsync(IAliveEntity target, IAliveEntity caster, List<BCardDto> cards, params BCardEffect[] effects)
    {
        var chances = cards.Where(c => effects.Contains(c.Effect()))
            .Select(c => new BCardDto
            {
                Type = BCardEffect.BuffChanceCausing.Type(),
                SubType = BCardEffect.BuffChanceCausing.SubType(),
                FirstData = c.FirstData,
                SecondData = c.SecondData,
            })
            .ToList();
        return chances.Count == 0 || !target.IsAlive ? Task.CompletedTask : buffService.InflictCardsAsync(target, caster, chances);
    }
}
