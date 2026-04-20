//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using Serilog;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class OpenShopHandler(ILogger logger)
    {
        [UsedImplicitly]
        public Task Handle(NrunRequestedEvent evt)
        {
            if (evt.Packet.Runner != NrunRunnerType.ProbabilityUIs || evt.Target is not NpcComponentBundle)
            {
                return Task.CompletedTask;
            }

            var raw = (byte)(evt.Packet.Type ?? 0);
            if (!Enum.IsDefined(typeof(WindowType), raw))
            {
                logger.Warning(
                    "n_run ProbabilityUIs requested unknown WindowType={Raw} (NPC visualId={VisualId}); extend WindowType or add a mapping",
                    raw,
                    evt.Target is NpcComponentBundle npc ? npc.VisualId : 0L);
                return Task.CompletedTask;
            }

            return evt.ClientSession.SendPacketAsync(new WopenPacket
            {
                Type = (WindowType)raw,
            });
        }
    }
}
