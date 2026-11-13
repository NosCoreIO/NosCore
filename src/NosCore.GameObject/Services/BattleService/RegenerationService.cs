//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.GameObject.Ecs;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Algorithm.RecoveryRateService;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.BattleService;

public sealed class RegenerationService(
    IClock clock,
    EquipmentService.IEquipmentStatsService equipmentStatsService,
    IBuffService buffService,
    SkillService.IPassiveSkillService passiveSkillService,
    IRecoveryRateService recoveryRates) : IRegenerationService, ISingletonService
{
    // Flat points added to each regeneration tick, sitting or standing alike.
    private (int Hp, int Mp) RecoveryBonus(PlayerComponentBundle character)
    {
        int hp = 0, mp = 0;
        var cards = equipmentStatsService.Resolve(character).BCards
            .Concat(buffService.GetActiveBuffs(character).SelectMany(b => b.BCards))
            .Concat(passiveSkillService.GetBCards(character));
        foreach (var card in cards)
        {
            switch (card.Effect())
            {
                case BCardEffect.RecoveryHpRecoveryIncreased: hp += BattleStatsProvider.ScaleByLevel(card, character.Level); break;
                case BCardEffect.RecoveryHpRecoveryDecreased: hp -= BattleStatsProvider.ScaleByLevel(card, character.Level); break;
                case BCardEffect.RecoveryMpRecoveryIncreased: mp += BattleStatsProvider.ScaleByLevel(card, character.Level); break;
                case BCardEffect.RecoveryMpRecoveryDecreased: mp -= BattleStatsProvider.ScaleByLevel(card, character.Level); break;
            }
        }

        return (hp, mp);
    }

    public void NotifyDamaged(PlayerComponentBundle character)
    {
        character.LastDamaged = clock.GetCurrentInstant();
    }

    public async Task RegenerateAsync(PlayerComponentBundle character)
    {
        if (!character.IsAlive)
        {
            return;
        }

        var hpRate = recoveryRates.GetRestingHpRate(character.Class);
        var mpRate = recoveryRates.GetRestingMpRate(character.Class);
        if (!character.IsSitting)
        {
            hpRate = recoveryRates.GetStandingRate(hpRate, character.Level);
            mpRate = recoveryRates.GetStandingRate(mpRate, character.Level);
        }

        var (hpBonus, mpBonus) = RecoveryBonus(character);
        hpRate += hpBonus;
        mpRate += mpBonus;

        var changed = false;
        if (character.Hp < character.MaxHp && hpRate > 0)
        {
            character.Hp = Math.Min(character.MaxHp, character.Hp + hpRate);
            changed = true;
        }

        if (character.Mp < character.MaxMp && mpRate > 0)
        {
            character.Mp = Math.Min(character.MaxMp, character.Mp + mpRate);
            changed = true;
        }

        if (changed)
        {
            await character.SendPacketAsync(character.GenerateStat());
        }
    }
}
