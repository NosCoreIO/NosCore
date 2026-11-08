//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class RequestNpcPacketHandler(ILogger<RequestNpcPacketHandler> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry,
            Wolverine.IMessageBus messageBus)
        : PacketHandler<RequestNpcPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(RequestNpcPacket requestNpcPacket, ClientSession clientSession)
        {
            switch (requestNpcPacket.Type)
            {
                case VisualType.Player:
                    if (sessionRegistry.TryGetCharacter(s => s.VisualId == requestNpcPacket.TargetId, out var player))
                    {
                        player.Requests[typeof(NpcDialogRequestSubject)].OnNext(new RequestData(clientSession));
                        return;
                    }
                    break;
                case VisualType.Npc:
                    var npc = clientSession.Character.MapInstance.FindNpc(s => s.VisualId == requestNpcPacket.TargetId);
                    if (npc.HasValue)
                    {
                        npc.Value.Requests[typeof(NpcDialogRequestSubject)].OnNext(new RequestData(clientSession));
                        await messageBus.PublishAsync(new NpcTalkedEvent(clientSession.Character, npc.Value));
                        return;
                    }
                    break;
                default:
                    logger.LogError(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN], requestNpcPacket.Type);
                    return;
            }

            logger.LogError(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
        }
    }
}
