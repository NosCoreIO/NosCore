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
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.EquipmentService;

namespace NosCore.GameObject.Services.BattleService;

// Gear and buffs that give a chance, on a landed hit, to put a card on the opponent or on
// oneself: FirstData is the chance, SecondData the card.
public sealed class OnHitBuffService(IBuffService buffService, IEquipmentStatsService equipmentStatsService)
    : IOnHitBuffService
{
    public async Task TriggerAsync(IAliveEntity attacker, IAliveEntity defender, bool defenderKilled)
    {
        var attackerCards = Cards(attacker);
        var defenderCards = Cards(defender);
        if (!defenderKilled)
        {
            await InflictAsync(defender, attacker, attackerCards, BCardEffect.Type95ChanceCausingXBuffOnEnemyOnAttack);
            await InflictAsync(defender, defender, defenderCards, BCardEffect.Type95ChanceReceivingXBuffOnDefence);
        }

        await InflictAsync(attacker, attacker, attackerCards, BCardEffect.Type95ChanceReceivingXBuffOnAttack);
        await InflictAsync(attacker, defender, defenderCards, BCardEffect.Type95ChanceCausingXBuffOnEnemyOnDefence);
    }

    private List<BCardDto> Cards(IAliveEntity entity) =>
        buffService.GetActiveBuffs(entity).SelectMany(b => b.BCards)
            .Concat(equipmentStatsService.Resolve(entity).BCards ?? [])
            .ToList();

    private Task InflictAsync(IAliveEntity target, IAliveEntity caster, List<BCardDto> cards, BCardEffect effect)
    {
        var chances = cards.Where(c => c.Effect() == effect)
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
