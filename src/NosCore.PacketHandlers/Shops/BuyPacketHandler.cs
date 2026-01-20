//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class BuyPacketHandler(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry, IOptions<WorldConfiguration> worldConfiguration)
        : PacketHandler<BuyPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(BuyPacket buyPacket, ClientSession clientSession)
        {
            IAliveEntity? aliveEntity;
            switch (buyPacket.VisualType)
            {
                case VisualType.Player:
                    aliveEntity = sessionRegistry.GetCharacter(s => s.VisualId == buyPacket.VisualId);
                    break;
                case VisualType.Npc:
                    aliveEntity = clientSession.Character.MapInstance.Npcs.Find(s => s.VisualId == buyPacket.VisualId);
                    break;

                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        buyPacket.VisualType);
                    return Task.CompletedTask;
            }

            if (aliveEntity != null)
            {
                return clientSession.Character.BuyAsync(aliveEntity.Shop!, buyPacket.Slot, buyPacket.Amount, worldConfiguration);
            }

            logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
            return Task.CompletedTask;

        }
    }
}
