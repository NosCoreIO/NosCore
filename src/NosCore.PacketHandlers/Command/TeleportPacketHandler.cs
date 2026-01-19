//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapChangeService;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class TeleportPacketHandler(ILogger logger, IMapChangeService mapChangeService,
            IGameLanguageLocalizer gameLanguageLocalizer, ISessionRegistry sessionRegistry)
        : PacketHandler<TeleportPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(TeleportPacket teleportPacket, ClientSession session)
        {
            var targetSession =
                sessionRegistry.GetCharacter(s =>
                    s.Name == teleportPacket.TeleportArgument);

            if (!short.TryParse(teleportPacket.TeleportArgument, out var mapId))
            {
                if (targetSession != null)
                {
                    return mapChangeService.ChangeMapInstanceAsync(session, targetSession.MapInstanceId, targetSession.MapX,
                        targetSession.MapY);
                }

                logger.Error(gameLanguageLocalizer[LanguageKey.USER_NOT_CONNECTED,
                    session.Account.Language]);
                return Task.CompletedTask;

            }

            return mapChangeService.ChangeMapAsync(session, mapId, teleportPacket.MapX, teleportPacket.MapY);
        }
    }
}
