//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class RequestNpcPacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry)
        : PacketHandler<RequestNpcPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(RequestNpcPacket requestNpcPacket, ClientSession clientSession)
        {
            switch (requestNpcPacket.Type)
            {
                case VisualType.Player:
                    if (sessionRegistry.TryGetCharacter(s => s.VisualId == requestNpcPacket.TargetId, out var player))
                    {
                        player.Requests[typeof(NpcDialogRequestSubject)].OnNext(new RequestData(clientSession));
                        return Task.CompletedTask;
                    }
                    break;
                case VisualType.Npc:
                    var npc = clientSession.Character.MapInstance.FindNpc(s => s.VisualId == requestNpcPacket.TargetId);
                    if (npc.HasValue)
                    {
                        npc.Value.Requests[typeof(NpcDialogRequestSubject)].OnNext(new RequestData(clientSession));
                        return Task.CompletedTask;
                    }
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN], requestNpcPacket.Type);
                    return Task.CompletedTask;
            }

            logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
            return Task.CompletedTask;
        }
    }
}
