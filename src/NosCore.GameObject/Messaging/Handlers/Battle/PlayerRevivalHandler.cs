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
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    // Player-specific death handling. Matches OpenNos ServerManager.ReviveFirstPosition
    // (the "return to town" branch of the revive dialog — which is also what you get
    // when the dialog times out / the player disconnects while dead):
    //   * 3s death pose (Normal mode only — ResumeInPlace skips this since the client
    //     just finished logging in and there's nothing to play a pose over)
    //   * Hp = Mp = 1 (not a full refill — you come back weak, like OpenNos)
    //   * Warp to the character's saved respawn map (characterDto.MapId/X/Y) which is
    //     the "saved location" the user returns to after declining the dialog
    //   * RevivePacket + StatPacket to refresh the HUD
    [UsedImplicitly]
    public sealed class PlayerRevivalHandler(
        ISessionRegistry sessionRegistry,
        IMapChangeService mapChangeService,
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
                    // Let the client play the death pose before we warp anywhere.
                    await Task.Delay(DeathPose).ConfigureAwait(false);
                }

                // OpenNos ReviveFirstPosition: coming back from a decline is deliberately
                // expensive — you get the bare minimum HP/MP and have to heal up.
                evt.Victim.Hp = 1;
                evt.Victim.Mp = 1;
                SetAlive(evt.Victim, true);

                if (evt.RevivalMode == RevivalMode.Normal)
                {
                    // Normal death → warp to the character's saved respawn (their
                    // persisted MapId/MapX/MapY). Matches the "return to town" dialog
                    // default. ResumeInPlace intentionally skips the warp — the char
                    // is already at their saved position from the login-time CreatePlayer.
                    await mapChangeService.ChangeMapAsync(session, character.MapId, character.MapX, character.MapY).ConfigureAwait(false);
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

        // PlayerComponentBundle's IsAlive setter routes through ECS; IAliveEntity doesn't
        // expose it, so we down-cast to the bundle types the combat pipeline uses.
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
