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
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Movement
{
    public class ClientDirPacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<ClientDirPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(ClientDirPacket dirpacket, ClientSession session)
        {
            IAliveEntity entity;
            switch (dirpacket.VisualType)
            {
                case VisualType.Player:
                    entity = session.Character;
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        dirpacket.VisualType);
                    return Task.CompletedTask;
            }

            return entity.ChangeDirAsync(dirpacket.Direction);
        }
    }
}
