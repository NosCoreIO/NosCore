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
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class RequestNpcPacketHandler(ILogger<RequestNpcPacketHandler> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry,
            Wolverine.IMessageBus messageBus, INpcDialogService npcDialogService)
        : PacketHandler<RequestNpcPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(RequestNpcPacket requestNpcPacket, ClientSession clientSession)
        {
            var character = clientSession.Character;
            switch (requestNpcPacket.Type)
            {
                case VisualType.Player:
                    if (sessionRegistry.TryGetCharacter(s => s.VisualId == requestNpcPacket.TargetId, out var player)
                        && npcDialogService.IsWithinReach(character, player))
                    {
                        npcDialogService.Open(character, player, NpcDialogWindow.Shop);
                        player.Requests[typeof(NpcDialogRequestSubject)].OnNext(new RequestData(clientSession));
                        return;
                    }
                    break;
                case VisualType.Npc:
                    var npc = character.MapInstance.FindNpc(s => s.VisualId == requestNpcPacket.TargetId);
                    if (npc is { } found && npcDialogService.IsWithinReach(character, found))
                    {
                        npcDialogService.Open(character, found, NpcDialogWindow.Conversation);
                        found.Requests[typeof(NpcDialogRequestSubject)].OnNext(new RequestData(clientSession));
                        await messageBus.PublishAsync(new NpcTalkedEvent(character, found));
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
