//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    // Monster kills are driven by the closing SuPacket (alive=0, hp%=0) — the client
    // plays the death animation from that alone and auto-despawns the sprite. Sending
    // an OutPacket here would cut the animation short, so we only clear aggro and
    // schedule the respawn. Player deaths are handled by the revive/warp flow.
    [UsedImplicitly]
    public sealed class MonsterRespawnHandler(IAggroService aggroService, ILogger<MonsterRespawnHandler> logger)
    {
        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is not MonsterComponentBundle monster) return Task.CompletedTask;
            if (monster.NpcMonster == null) return Task.CompletedTask;
            if (monster.MapInstance == null) return Task.CompletedTask;

            aggroService.Clear(monster);

            var respawnMs = Math.Max(1000, monster.NpcMonster.RespawnTime);
            _ = RespawnAfterDelayAsync(monster, respawnMs);
            return Task.CompletedTask;
        }

        private async Task RespawnAfterDelayAsync(MonsterComponentBundle monster, int delayMs)
        {
            try
            {
                await Task.Delay(delayMs).ConfigureAwait(false);
                // Map may have been disposed between death and respawn — bail if so.
                if (monster.MapInstance == null) return;

                monster.Hp = monster.MaxHp;
                monster.Mp = monster.MaxMp;
                monster.IsAlive = true;
                monster.PositionX = monster.FirstX;
                monster.PositionY = monster.FirstY;
                monster.HitList.Clear();

                await monster.MapInstance.SendPacketAsync(monster.GenerateIn()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to respawn monster {VisualId}", monster.VisualId);
            }
        }
    }
}
