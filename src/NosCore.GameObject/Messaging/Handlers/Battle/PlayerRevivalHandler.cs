//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
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
    // Player-specific death handling. Monster deaths are handled by MonsterRespawnHandler;
    // for players we give them a short "death pose" window, warp them back to a respawn
    // point, refill vitals, and tell the client to play the resurrect animation via
    // RevivePacket. The flow matches the auto-respawn behaviour from the OpenNos
    // revive dialog's "return to town" option — a dialog for pick-your-destination can
    // be layered on later without changing this handler's contract.
    [UsedImplicitly]
    public sealed class PlayerRevivalHandler(
        ISessionRegistry sessionRegistry,
        IMapChangeService mapChangeService,
        ILogger logger)
    {
        // Default respawn point — Nosville town spawn. A later iteration can read a
        // per-character "home" or nearest save point instead of this constant.
        private const short RespawnMapId = 1;
        private const short RespawnX = 78;
        private const short RespawnY = 114;
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
                // Let the client play the death animation + show the DiePacket
                // result before we start teleporting. 3s matches the standard
                // NosTale death pose duration.
                await Task.Delay(DeathPose).ConfigureAwait(false);

                // Restore vitals on the current entity so the map-change clone inherits
                // a living state and the downstream broadcasts (InPacket, StatPacket)
                // don't show a corpse with full HP. We flip the underlying ECS flag
                // explicitly since HP-setters don't sync IsAlive (see HitQueue for the
                // same reason).
                evt.Victim.Hp = evt.Victim.MaxHp;
                evt.Victim.Mp = evt.Victim.MaxMp;
                SetAlive(evt.Victim, true);

                await mapChangeService.ChangeMapAsync(session, RespawnMapId, RespawnX, RespawnY).ConfigureAwait(false);

                // RevivePacket tells the client "you're coming back" — used to swap the
                // revive dialog back to normal HUD and play the resurrect effect.
                // VisualId stays the same; Data = 0 is the normal revive (no lives
                // counter, non-instance context).
                await session.SendPacketAsync(new RevivePacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.VisualId,
                    Data = 0,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Player revival failed for {CharacterId}", character.CharacterId);
            }
        }

        // PlayerComponentBundle's IsAlive setter routes through ECS; the IAliveEntity
        // interface doesn't expose it, so we down-cast to the concrete bundle types
        // the combat pipeline deals with.
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
