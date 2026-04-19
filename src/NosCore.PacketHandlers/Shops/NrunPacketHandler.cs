//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.PacketHandlers.Shops
{
    public class NrunPacketHandler(ILogger logger, IMessageBus messageBus,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry)
        : PacketHandler<NrunPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(NrunPacket nRunPacket, ClientSession clientSession)
        {
            var forceNull = false;
            IAliveEntity? aliveEntity;
            switch (nRunPacket.VisualType)
            {
                case VisualType.Player:
<<<<<<< HEAD
                    aliveEntity = sessionRegistry.TryGetCharacter(s => s.VisualId == nRunPacket.VisualId, out var runner) ? runner : null;
                    break;
                case VisualType.Npc:
                    aliveEntity = clientSession.Character.MapInstance.FindNpc(s => s.VisualId == nRunPacket.VisualId);
=======
                    entity = sessionRegistry.TryGetCharacter(s => s.VisualId == nRunPacket.VisualId, out var runner)
                        ? (object)runner
                        : null;
                    break;
                case VisualType.Npc:
                    var npc = clientSession.Character.MapInstance.FindNpc(s => s.VisualId == nRunPacket.VisualId);
                    entity = npc.HasValue ? (object)npc.Value : null;
>>>>>>> a29926f2 (Migrate NRunService to Wolverine + decouple NPC dialog dictionary key)
                    break;
                case null:
                    aliveEntity = null;
                    forceNull = true;
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN], nRunPacket.Type);
                    return Task.CompletedTask;
            }

<<<<<<< HEAD
            if ((aliveEntity == null) && !forceNull)
=======
            if (entity == null && !forceNull)
>>>>>>> a29926f2 (Migrate NRunService to Wolverine + decouple NPC dialog dictionary key)
            {
                logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                return Task.CompletedTask;
            }

<<<<<<< HEAD
            await nRunRunnerService.NRunLaunchAsync(clientSession, new Tuple<IAliveEntity, NrunPacket>(aliveEntity!, nRunPacket));
=======
            return messageBus.PublishAsync(new NrunRequestedEvent(clientSession, entity, nRunPacket)).AsTask();
>>>>>>> a29926f2 (Migrate NRunService to Wolverine + decouple NPC dialog dictionary key)
        }
    }
}
