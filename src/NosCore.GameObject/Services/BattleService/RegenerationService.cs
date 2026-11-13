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
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Services.BattleService;

// Standing regen is gated on no damage for 4 seconds; without it the bar refills mid-fight.
public sealed class RegenerationService(
    ISessionRegistry sessionRegistry,
    IClock clock,
    EquipmentService.IEquipmentStatsService equipmentStatsService,
    IBuffService buffService,
    SkillService.IPassiveSkillService passiveSkillService,
    IRecoveryRateService recoveryRates,
    ILogger<RegenerationService> logger) : IRegenerationService, ISingletonService
{
    private static readonly Duration SittingInterval = Duration.FromMilliseconds(1500);
    private static readonly Duration StandingInterval = Duration.FromMilliseconds(2000);
    private static readonly Duration StandingDefenceGrace = Duration.FromSeconds(4);

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

    public async Task TickAsync(MapInstance mapInstance)
    {
        try
        {
            var now = clock.GetCurrentInstant();
            foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(mapInstance.MapInstanceId))
            {
                if (!session.HasPlayerEntity) continue;
                var character = session.Character;
                if (!character.IsAlive) continue;
                if (character.Hp >= character.MaxHp && character.Mp >= character.MaxMp) continue;

                var interval = character.IsSitting ? SittingInterval : StandingInterval;
                if (character.LastRegen == Instant.MinValue)
                {
                    character.LastRegen = now;
                }

                if (now - character.LastRegen < interval) continue;

                var hpRate = recoveryRates.GetRestingHpRate(character.Class);
                var mpRate = recoveryRates.GetRestingMpRate(character.Class);
                if (!character.IsSitting)
                {
                    // Standing regen only kicks in once 4s have elapsed since the
                    // last incoming hit. Before that the rates are zero so the bars
                    // stay put while you're being swung at.
                    if (now - character.LastDamaged < StandingDefenceGrace)
                    {
                        character.LastRegen = now;
                        continue;
                    }

                    hpRate = recoveryRates.GetStandingRate(hpRate, character.Level);
                    mpRate = recoveryRates.GetStandingRate(mpRate, character.Level);
                }

                character.LastRegen = now;

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
                    await session.SendPacketAsync(character.GenerateStat());
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Regeneration tick failed for map {MapId}", mapInstance.Map.MapId);
        }
    }
}
