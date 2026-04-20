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

namespace NosCore.GameObject.Services.BattleService;

// Per-class, per-posture regen rates from OpenNos CharacterHelper.HpHealth /
// HpHealthStand / MpHealth / MpHealthStand. Sitting regen ticks every 1500ms,
// standing every 2000ms (OpenNos default cadence). Standing regen is gated on
// not having used a skill / taken damage in the last 4s, but we keep it
// unconditional here for v1 — skill cooldown enforcement already discourages
// spam, and tracking LastDefence would require a new ECS component.
public sealed class RegenerationService(
    ISessionRegistry sessionRegistry,
    IClock clock,
    Serilog.ILogger logger) : IRegenerationService, ISingletonService
{
    private static readonly Duration SittingInterval = Duration.FromMilliseconds(1500);
    private static readonly Duration StandingInterval = Duration.FromMilliseconds(2000);

    // HpHealth[class] sitting rate; HpHealthStand[class] standing rate.
    // Index matches CharacterClassType numeric value (Adventurer=0, Swordsman=1,
    // Archer=2, Mage=3, MartialArtist=4 — martial artist treated like Swordsman).
    private static readonly int[] HpSittingRate = { 30, 90, 60, 30, 90 };
    private static readonly int[] HpStandingRate = { 25, 26, 32, 20, 26 };
    private static readonly int[] MpSittingRate = { 10, 30, 50, 80, 30 };
    private static readonly int[] MpStandingRate = { 8, 10, 20, 40, 10 };

    // Per-character last-regen timestamps. Concurrent because map-life-loops for
    // different maps can tick their players in parallel.
    private readonly ConcurrentDictionary<long, Instant> _lastRegen = new();

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
                _lastRegen[character.CharacterId] = now;

                var classIndex = Math.Clamp((int)character.Class, 0, HpSittingRate.Length - 1);
                var hpRate = character.IsSitting ? HpSittingRate[classIndex] : HpStandingRate[classIndex];
                var mpRate = character.IsSitting ? MpSittingRate[classIndex] : MpStandingRate[classIndex];

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
            logger.Warning(ex, "Regeneration tick failed for map {MapId}", mapInstance.Map.MapId);
        }
    }
}
