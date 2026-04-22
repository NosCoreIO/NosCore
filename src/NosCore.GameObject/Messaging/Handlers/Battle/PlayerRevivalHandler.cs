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
using NosCore.GameObject.Messaging.Events;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    // Player death flow: zero HP/MP, apply the dignity penalty, broadcast the
    // refreshed stats, then ask the client which revive branch to take via a `dlgi`
    // dialog. The client routes the yes/no answer back as a `revival` client packet,
    // which RevivalPacketHandler turns into the actual tp + revive + stat sequence.
    //
    // Dialog text id depends on level:
    //   - lvl <= 20: ContinueHereFree (570)      — revive in place is free.
    //   - lvl >  20: ContinueHereTenSeeds (571)  — costs 10x Seed of Power (item 1012).
    //
    // TODO: post-revive protection (card 684, ~30s invuln) isn't granted — the
    // buff/card application layer isn't wired up yet. See death.txt line 6352.
    [UsedImplicitly]
    public sealed class PlayerRevivalHandler(ILogger logger)
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
                player.IsAlive = false;
                player.Hp = 0;
                player.Mp = 0;

                player.Dignity = (short)Math.Max(DignityFloor, player.Dignity - DignityLossPerDeath);

                await player.SendPacketAsync(player.GenerateStat()).ConfigureAwait(false);
                await player.SendPacketAsync(player.GenerateFd()).ConfigureAwait(false);

                var question = player.Level > 20
                    ? Game18NConstString.ContinueHereTenSeeds
                    : Game18NConstString.ContinueHereFree;

                await player.SendPacketAsync(new DlgiPacket
                {
                    YesPacket = new RevivalPacket { Type = 0 },
                    NoPacket = new RevivalPacket { Type = 1 },
                    Question = question,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "PlayerRevivalHandler failed for {CharacterId}", player.CharacterId);
            }
        }
    }
}
