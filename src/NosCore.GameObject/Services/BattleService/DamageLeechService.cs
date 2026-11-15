//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.EquipmentService;

namespace NosCore.GameObject.Services.BattleService;

public sealed class DamageLeechService(IBuffService buffService, IEquipmentStatsService equipmentStatsService,
    INpcCombatCatalog npcCombatCatalog, IRandomProvider random)
    : IDamageLeechService
{
    private const int MaxLossPercentOfMaxHp = 50;

    public async Task ApplyAsync(IAliveEntity attacker, int damageDealt)
    {
        if (damageDealt <= 0 || !attacker.IsAlive)
        {
            return;
        }

        var cards = buffService.GetActiveBuffs(attacker).SelectMany(b => b.BCards)
            .Concat(equipmentStatsService.Resolve(attacker).BCards ?? [])
            .ToList();
        var gain = (long)damageDealt * Sum(cards, BCardEffect.ReflectionHpIncreased, attacker.Level) / 100;
        var loss = Math.Min((long)damageDealt * Sum(cards, BCardEffect.ReflectionHpDecreased, attacker.Level) / 100,
            (long)attacker.MaxHp * MaxLossPercentOfMaxHp / 100);
        if (gain == 0 && loss == 0)
        {
            return;
        }

        var hp = (int)Math.Clamp(attacker.Hp + gain - loss, 1, attacker.MaxHp);
        if (hp == attacker.Hp)
        {
            return;
        }

        attacker.Hp = hp;
        if (attacker is PlayerComponentBundle player)
        {
            await player.SendPacketAsync(player.GenerateStat());
        }
    }

    public async Task DrainAsync(IAliveEntity attacker, IAliveEntity target, IReadOnlyList<BCardDto> skillCards)
    {
        if (!attacker.IsAlive || !target.IsAlive)
        {
            return;
        }

        var hpBefore = (attacker.Hp, attacker.Mp, target.Hp, target.Mp);
        foreach (var card in DrainSources(attacker, skillCards))
        {
            switch (card.Effect())
            {
                case BCardEffect.DrainAndStealLeechEnemyHp when Rolls(card.FirstData):
                    var hp = Math.Min(card.SecondData, target.Hp - 1);
                    target.Hp -= Math.Max(0, hp);
                    attacker.Hp = Math.Min(attacker.MaxHp, attacker.Hp + Math.Max(0, hp));
                    break;
                case BCardEffect.DrainAndStealLeechEnemyMp when Rolls(card.FirstData):
                    var mp = Math.Min(card.SecondData, target.Mp);
                    target.Mp -= Math.Max(0, mp);
                    attacker.Mp = Math.Min(attacker.MaxMp, attacker.Mp + Math.Max(0, mp));
                    break;
                case BCardEffect.DrainAndStealConvertEnemyHptoMp:
                    var converted = Math.Min(BattleStatsProvider.ScaleByLevel(card, attacker.Level), target.Hp - 1);
                    target.Hp -= Math.Max(0, converted);
                    attacker.Mp = Math.Min(attacker.MaxMp, attacker.Mp + Math.Max(0, converted));
                    break;
            }
        }

        if (hpBefore == (attacker.Hp, attacker.Mp, target.Hp, target.Mp))
        {
            return;
        }

        foreach (var entity in new[] { attacker, target })
        {
            if (entity is PlayerComponentBundle player)
            {
                await player.SendPacketAsync(player.GenerateStat());
            }
        }
    }

    private IEnumerable<BCardDto> DrainSources(IAliveEntity attacker, IReadOnlyList<BCardDto> skillCards)
    {
        var passives = attacker is INonPlayableEntity { NpcMonster: { } monster } and not MateComponentBundle
            ? npcCombatCatalog.GetNpcBCards(monster.NpcMonsterVNum).Where(c => c.Slot == 0)
            : [];
        return skillCards
            .Concat(buffService.GetActiveBuffs(attacker).SelectMany(b => b.BCards))
            .Concat(equipmentStatsService.Resolve(attacker).BCards ?? [])
            .Concat(passives);
    }

    private bool Rolls(int percent) => percent > 0 && random.Next(0, 100) < percent;

    private static int Sum(List<BCardDto> cards, BCardEffect effect, int level) =>
        cards.Where(c => c.Effect() == effect).Sum(c => BattleStatsProvider.ScaleByLevel(c, level));
}
