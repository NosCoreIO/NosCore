//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.DigestionService;

public sealed class DigestionService(
    ISessionRegistry sessionRegistry,
    IClock clock,
    ILogger<DigestionService> logger) : IDigestionService, ISingletonService
{
    public const int FoodCap = 600;
    public const int EatEffect = 6000;
    public static readonly Duration TickInterval = Duration.FromSeconds(2);

    public async Task TickAsync(MapInstance mapInstance)
    {
        try
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
                    logger.LogWarning(ex, "Digestion tick failed for session {SessionId}", session.SessionId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Digestion tick failed for map {MapId}", mapInstance.Map.MapId);
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

        var hp = Math.Min(player.MaxHp, player.Hp + food.Hp + snack.Hp) - player.Hp;
        var mp = Math.Min(player.MaxMp, player.Mp + food.Mp + snack.Mp) - player.Mp;
        if (hp <= 0 && mp <= 0)
        {
            return;
        }

        player.Hp += Math.Max(hp, 0);
        player.Mp += Math.Max(mp, 0);
        if (hp > 0)
        {
            await player.MapInstance.SendPacketAsync(new RcPacket
            {
                VisualType = VisualType.Player,
                VisualId = player.VisualId,
                HealAmount = hp,
                Unknown = 0
            });
        }

        await player.SendPacketAsync(player.GenerateStat());
    }
}
