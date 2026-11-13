//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.IceBreakerService;
using NosCore.GameObject.Services.LandOfDeathService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    // Player death flow: zero HP/MP, apply the dignity penalty, broadcast the
    // refreshed stats, then ask the client which revive branch to take via a `dlgi`
    // dialog. The client routes the yes/no answer back as a `revival` client packet,
    // which RevivalPacketHandler turns into the actual tp + revive + stat sequence.
    //
    // A fall in an arena costs neither dignity nor reputation; the fighter is offered a paid revive
    // on the spot instead. A fall in an Instant Combat is out of it: back where they came from with 1 HP.
    // A fall in an Icebreaker freezes the player where they stand until it ends.
    // A fall in the Land of Death offers to spend a Saver on the spot, or goes back to the entrance.
    //
    // Dialog text id depends on level:
    //   - lvl <= 20: ContinueHereFree (570)      — revive in place is free.
    //   - lvl >  20: ContinueHereTenSeeds (571)  — costs 10x Seed of Power.
    [UsedImplicitly]
    public sealed class PlayerRevivalHandler(ILogger<PlayerRevivalHandler> logger, ISessionRegistry sessionRegistry,
        IMapChangeService mapChangeService, IIceBreakerService iceBreakerService,
        ILandOfDeathService landOfDeathService)
    {
        // Canonical NosTale fork default for dignity penalty on death.
        private const short DignityLossPerDeath = 50;
        private const short DignityFloor = -1000;

        [UsedImplicitly]
        public async Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim.VisualType != VisualType.Player) return;
            if (evt.Victim is not PlayerComponentBundle player) return;

            try
            {
                await player.OnMapAsync(() =>
                {
                    player.IsAlive = false;
                    player.Hp = 0;
                    player.Mp = 0;
                    return Task.CompletedTask;
                });

                if (player.MapInstance?.MapInstanceType == MapInstanceType.InstantCombatInstance)
                {
                    await LeaveInstantCombatAsync(player);
                    return;
                }

                if (player.MapInstance?.MapInstanceType == MapInstanceType.IceBreakerInstance)
                {
                    await iceBreakerService.FreezeAsync(player);
                    return;
                }

                if (player.MapInstance?.MapInstanceType == MapInstanceType.LodInstance)
                {
                    await landOfDeathService.AskReviveAsync(player);
                    return;
                }

                if (player.MapInstance?.MapInstanceType == MapInstanceType.ArenaInstance)
                {
                    await player.SendPacketAsync(player.GenerateStat());
                    await player.SendPacketAsync(new DlgiPacket
                    {
                        YesPacket = new RevivalPacket { Type = RevivalType.InArena },
                        NoPacket = new RevivalPacket { Type = RevivalType.AtSavePoint },
                        Question = Game18NConstString.AskRevive,
                    });
                    return;
                }

                player.Dignity = (short)Math.Max(DignityFloor, player.Dignity - DignityLossPerDeath);

                if (evt.Killer is PlayerComponentBundle killer && killer.CharacterId != player.CharacterId
                    && player.Reput >= 50000)
                {
                    var transfer = player.Level * 50L;
                    player.Reput -= transfer;
                    killer.Reput += transfer;
                    await killer.SendPacketAsync(killer.GenerateFd()).ConfigureAwait(false);
                }

                await player.SendPacketAsync(player.GenerateStat()).ConfigureAwait(false);
                await player.SendPacketAsync(player.GenerateFd()).ConfigureAwait(false);

                var question = player.Level > 20
                    ? Game18NConstString.ContinueHereTenSeeds
                    : Game18NConstString.ContinueHereFree;

                await player.SendPacketAsync(new DlgiPacket
                {
                    YesPacket = new RevivalPacket { Type = RevivalType.InPlace },
                    NoPacket = new RevivalPacket { Type = RevivalType.AtSavePoint },
                    Question = question,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "PlayerRevivalHandler failed for {CharacterId}", player.CharacterId);
            }
        }

        private async Task LeaveInstantCombatAsync(PlayerComponentBundle player)
        {
            if (sessionRegistry.GetSessionByCharacterId(player.CharacterId) is not { } session)
            {
                return;
            }

            await player.OnMapAsync(() =>
            {
                player.Hp = 1;
                player.Mp = 1;
                player.IsAlive = true;
                return Task.CompletedTask;
            });
            if (player.MapInstance is { } mapInstance)
            {
                await mapInstance.SendPacketAsync(new RevivePacket
                {
                    VisualType = VisualType.Player,
                    VisualId = player.VisualId,
                    Data = 0,
                });
            }

            await player.SendPacketAsync(player.GenerateStat());
            await mapChangeService.ChangeMapAsync(session, player.MapId, player.MapX, player.MapY);
        }
    }
}
