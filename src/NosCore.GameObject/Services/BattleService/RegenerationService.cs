//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NodaTime;
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
    ILogger<RegenerationService> logger) : IRegenerationService, ISingletonService
{
    private static readonly Duration SittingInterval = Duration.FromMilliseconds(1500);
    private static readonly Duration StandingInterval = Duration.FromMilliseconds(2000);
    private static readonly Duration StandingDefenceGrace = Duration.FromSeconds(4);

    private static readonly int[] HpSittingRate = { 30, 80, 60, 30, 70 };
    private static readonly int[] MpSittingRate = { 10, 30, 50, 80, 40 };

    /// <summary>
    /// On one's feet the rate is derived from the resting one, not looked up.
    /// </summary>
    /// <remarks>
    /// The factor is one half up to level 20 and steps down in bands after it. Multiply before
    /// dividing, or the band is lost to integer truncation.
    /// </remarks>
    public static int StandingRate(int restingRate, byte level)
    {
        var percent = level <= 20 ? 50
            : level <= 40 ? 40
            : level <= 60 ? 30
            : 20;
        return restingRate * percent / 100;
    }

    private readonly ConcurrentDictionary<long, Instant> _lastRegen = new();
    private readonly ConcurrentDictionary<long, Instant> _lastDefence = new();

    public void NotifyDamaged(long characterId)
    {
        _lastDefence[characterId] = clock.GetCurrentInstant();
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
                var last = _lastRegen.GetOrAdd(character.CharacterId, now);
                if (now - last < interval) continue;

                var classIndex = Math.Clamp((int)character.Class, 0, HpSittingRate.Length - 1);
                int hpRate, mpRate;
                if (character.IsSitting)
                {
                    hpRate = HpSittingRate[classIndex];
                    mpRate = MpSittingRate[classIndex];
                }
                else
                {
                    // Standing regen only kicks in once 4s have elapsed since the
                    // last incoming hit. Before that the rates are zero so the bars
                    // stay put while you're being swung at.
                    if (_lastDefence.TryGetValue(character.CharacterId, out var lastDefence)
                        && now - lastDefence < StandingDefenceGrace)
                    {
                        _lastRegen[character.CharacterId] = now;
                        continue;
                    }

                    hpRate = StandingRate(HpSittingRate[classIndex], character.Level);
                    mpRate = StandingRate(MpSittingRate[classIndex], character.Level);
                }

                _lastRegen[character.CharacterId] = now;

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
                    await session.SendPacketAsync(character.GenerateStat()).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Regeneration tick failed for map {MapId}", mapInstance.Map.MapId);
        }
    }
}
