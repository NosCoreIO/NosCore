//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.DignityService;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Entities.Extensions;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Entities.Interfaces;
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
    public class ShoppingPacketHandler(ILogger logger, IDignityService dignityService,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry)
        : PacketHandler<ShoppingPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ShoppingPacket shoppingPacket, ClientSession clientSession)
        {
            var percent = 0d;
            IAliveEntity? aliveEntity;
            switch (shoppingPacket.VisualType)
            {
                case VisualType.Player:
                    aliveEntity = sessionRegistry.TryGetCharacter(s => s.VisualId == shoppingPacket.VisualId, out var shopper) ? shopper : null;
                    break;
                case VisualType.Npc:

                    percent = (dignityService.GetLevelFromDignity(clientSession.Character.Dignity)) switch
                    {
                        DignityType.Dreadful => 1.1,
                        DignityType.Unqualified => 1.2,
                        DignityType.Failed => 1.5,
                        DignityType.Useless => 1.5,
                        _ => 1.0,
                    };
                    aliveEntity =
                        clientSession.Character.MapInstance.Npcs.Find(s => s.VisualId == shoppingPacket.VisualId);
                    break;

                default:
                    logger.Error(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        shoppingPacket.VisualType);
                    return;
            }

            if (aliveEntity == null)
            {
                logger.Error(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                return;
            }

            if (aliveEntity.Shop?.ShopItems.IsEmpty == false)
            {
                await clientSession.SendPacketAsync(aliveEntity.GenerateNInv(percent, shoppingPacket.ShopType))
                    ;
            }
        }
    }
}
