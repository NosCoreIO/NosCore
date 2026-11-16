//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Core.Observability;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.DigestionService;

public sealed class DigestionService(
    ISessionRegistry sessionRegistry,
    IClock clock,
    ILogger<DigestionService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IDigestionService, ISingletonService
{
    public const int FoodCap = 600;
    public const int EatEffect = 6000;
    public static readonly Duration TickInterval = Duration.FromSeconds(2);

    public async Task TickAsync(MapInstance mapInstance)
    {
        foreach (var session in sessionRegistry.GetClientSessionsByMapInstance(mapInstance.MapInstanceId))
        {
            try
            {
                if (session.HasPlayerEntity)
                {
                    await TickPlayerAsync(session.Character);
                }
            }
            catch (Exception ex)
            {
                NosCoreMetrics.Failures.Add(1, new KeyValuePair<string, object?>("boundary", "map_entity"));
                logger.LogWarning(ex, logLanguage[LogLanguageKey.DIGESTION_SESSION_TICK_FAILED], session.SessionId);
            }
        }
    }

    public async Task TickPlayerAsync(PlayerComponentBundle player)
    {
        var state = player.Digestion;
        (int Hp, int Mp) food;
        (int Hp, int Mp) snack;

        await state.Lock.WaitAsync();
        try
        {
            if (!player.IsAlive)
            {
                state.Food.Clear();
                state.Snack.Clear();
                return;
            }

            if (!player.IsSitting)
            {
                state.Food.Clear();
            }

            var now = clock.GetCurrentInstant();
            if ((state.Food.IsEmpty && state.Snack.IsEmpty) || now - state.LastTick < TickInterval)
            {
                return;
            }

            food = state.Food.Tick(FoodCap);
            snack = state.Snack.Tick();
            // Stepping by the interval keeps a two second rhythm on a 400 ms map loop; after a
            // stall it restarts from now so the missed ticks do not land all at once.
            state.LastTick = now - state.LastTick < TickInterval * 2 ? state.LastTick + TickInterval : now;
        }
        finally
        {
            state.Lock.Release();
        }

        await player.HealAsync(food.Hp + snack.Hp, food.Mp + snack.Mp);
    }
}
