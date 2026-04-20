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
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    // When a monster dies we drop an OutPacket so the client despawns the corpse, then
    // schedule a delayed revive after NpcMonsterDto.RespawnTime. The respawn resets HP,
    // clears aggro, and re-broadcasts an InPacket from the monster's original spawn
    // point. Player deaths are ignored here — they're handled by the revive/warp flow.
    [UsedImplicitly]
    public sealed class MonsterRespawnHandler(IAggroService aggroService, ILogger logger)
    {
        [UsedImplicitly]
        public async Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is not MonsterComponentBundle monster) return;
            if (monster.NpcMonster == null) return;
            var mapInstance = monster.MapInstance;
            if (mapInstance == null) return;

            aggroService.Clear(monster);

            // Despawn first so the client removes the sprite rather than showing a
            // 0-HP idle monster. We reuse VisualType.Monster since OutPacket only cares
            // about the id + type.
            try
            {
                await mapInstance.SendPacketAsync(new OutPacket
                {
                    VisualType = VisualType.Monster,
                    VisualId = monster.VisualId,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to despawn monster {VisualId}", monster.VisualId);
            }

            var respawnMs = Math.Max(1000, monster.NpcMonster.RespawnTime);
            _ = RespawnAfterDelayAsync(monster, respawnMs);
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
                logger.Warning(ex, "Failed to respawn monster {VisualId}", monster.VisualId);
            }
        }
    }
}
