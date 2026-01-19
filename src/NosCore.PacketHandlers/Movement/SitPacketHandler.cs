//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Movement
{
    public class SitPacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry)
        : PacketHandler<SitPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(SitPacket sitpacket, ClientSession clientSession)
        {
            return Task.WhenAll(sitpacket.Users!.Select(u =>
            {
                IAliveEntity entity;

                switch (u!.VisualType)
                {
                    case VisualType.Player:
                        entity = sessionRegistry.GetCharacter(s => s.VisualId == u.VisualId)!;
                        if (entity.VisualId != clientSession.Character.VisualId)
                        {
                            logger.Error(
                                logLanguage[LogLanguageKey.DIRECT_ACCESS_OBJECT_DETECTED],
                                clientSession.Character, sitpacket);
                            return Task.CompletedTask;
                        }

                        break;
                    default:
                        logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                            u.VisualType);
                        return Task.CompletedTask;
                }

                return entity.RestAsync();
            }));
        }
    }
}
