//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;

namespace NosCore.PacketHandlers.Shops
{
    public class NrunPacketHandler(
            ILogger<NrunPacketHandler> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry,
            IEnumerable<INrunEventHandler> handlers)
        : PacketHandler<NrunPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(NrunPacket packet, ClientSession session)
        {
            var handler = handlers.FirstOrDefault(h => h.Runner == packet.Runner);
            if (handler is null)
            {
                logger.LogDebug("Unhandled n_run runner {Runner}", packet.Runner);
                return Task.CompletedTask;
            }

            IAliveEntity? target;
            switch (packet.VisualType)
            {
                case VisualType.Player:
                    target = sessionRegistry.TryGetCharacter(s => s.VisualId == packet.VisualId, out var runner) ? runner : null;
                    break;
                case VisualType.Npc:
                    target = session.Character.MapInstance.FindNpc(s => s.VisualId == packet.VisualId);
                    break;
                case null:
                    target = null;
                    break;
                default:
                    logger.LogError(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN], packet.Type);
                    return Task.CompletedTask;
            }

            return handler.HandleAsync(session, target, packet);
        }
    }
}
