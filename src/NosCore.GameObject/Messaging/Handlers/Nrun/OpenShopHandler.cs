//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using Serilog;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class OpenShopHandler(ILogger logger) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.ProbabilityUIs;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not NpcComponentBundle npc)
            {
                return Task.CompletedTask;
            }

            var raw = (byte)(packet.Type ?? 0);
            if (!Enum.IsDefined(typeof(WindowType), raw))
            {
                logger.Warning(
                    "n_run ProbabilityUIs requested unknown WindowType={Raw} (NPC visualId={VisualId}); extend WindowType or add a mapping",
                    raw,
                    npc.VisualId);
                return Task.CompletedTask;
            }

            return session.SendPacketAsync(new WopenPacket
            {
                Type = (WindowType)raw,
            });
        }
    }
}
