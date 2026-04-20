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
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class PlayerRevivalHandler(
        ISessionRegistry sessionRegistry,
        IMapChangeService mapChangeService,
        IRespawnService respawnService,
        ILogger logger)
    {
        private static readonly TimeSpan DeathPose = TimeSpan.FromSeconds(3);

        [UsedImplicitly]
        public async Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim.VisualType != VisualType.Player) return;
            if (evt.Victim is not ICharacterEntity character) return;

            var session = sessionRegistry.GetSessionByCharacterId(character.CharacterId);
            if (session == null) return;

            try
            {
                if (evt.RevivalMode == RevivalMode.Normal)
                {
                    await Task.Delay(DeathPose).ConfigureAwait(false);
                }

                evt.Victim.Hp = 1;
                evt.Victim.Mp = 1;
                SetAlive(evt.Victim, true);

                if (evt.RevivalMode == RevivalMode.Normal)
                {
                    var deathMapId = character.MapInstance?.Map.MapId ?? character.MapId;
                    var respawnMapTypeId = respawnService.ResolveRespawnMapTypeId(deathMapId);
                    var (mapId, x, y) = respawnService.GetRespawnLocation(character, respawnMapTypeId);
                    await mapChangeService.ChangeMapAsync(session, mapId, x, y).ConfigureAwait(false);
                }

                await session.SendPacketAsync(new RevivePacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.VisualId,
                    Data = 0,
                }).ConfigureAwait(false);

                if (session.HasPlayerEntity)
                {
                    var refreshed = session.Character;
                    await session.SendPacketAsync(refreshed.GenerateStat()).ConfigureAwait(false);
                    if (refreshed.MapInstance != null)
                    {
                        await refreshed.MapInstance.SendPacketAsync(refreshed.GenerateCond()).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Player revival failed for {CharacterId}", character.CharacterId);
            }
        }

        private static void SetAlive(IAliveEntity entity, bool alive)
        {
            switch (entity)
            {
                case PlayerComponentBundle p: p.IsAlive = alive; break;
                case MonsterComponentBundle m: m.IsAlive = alive; break;
                case NpcComponentBundle n: n.IsAlive = alive; break;
            }
        }
    }
}
