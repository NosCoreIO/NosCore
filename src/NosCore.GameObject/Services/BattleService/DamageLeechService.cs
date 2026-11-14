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

public sealed class DamageLeechService(IBuffService buffService, IEquipmentStatsService equipmentStatsService)
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

    private static int Sum(List<BCardDto> cards, BCardEffect effect, int level) =>
        cards.Where(c => c.Effect() == effect).Sum(c => BattleStatsProvider.ScaleByLevel(c, level));
}
