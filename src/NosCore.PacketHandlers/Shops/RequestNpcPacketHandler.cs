//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.NRunService;
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
            IRequestableEntity? requestableEntity;
            switch (requestNpcPacket.Type)
            {
                case VisualType.Player:
                    requestableEntity = sessionRegistry.GetCharacter(s => s.VisualId == requestNpcPacket.TargetId);
                    break;
                case VisualType.Npc:
                    requestableEntity =
                        clientSession.Character.MapInstance.Npcs.Find(s => s.VisualId == requestNpcPacket.TargetId);
                    break;

                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        requestNpcPacket.Type);
                    return Task.CompletedTask;
            }

            if (requestableEntity == null)
            {
                logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                return Task.CompletedTask;
            }

            requestableEntity.Requests[typeof(INrunEventHandler)].OnNext(new RequestData(clientSession));
            return Task.CompletedTask;
        }
    }
}
