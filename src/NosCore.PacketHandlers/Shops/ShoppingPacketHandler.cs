//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.DignityService;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class ShoppingPacketHandler(ILogger<ShoppingPacketHandler> logger, IDignityService dignityService,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry,
            INpcDialogService npcDialogService, IFamilyShopService familyShopService)
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
                        clientSession.Character.MapInstance.FindNpc(s => s.VisualId == shoppingPacket.VisualId);
                    break;

                default:
                    logger.LogError(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        shoppingPacket.VisualType);
                    return;
            }

            if (aliveEntity == null)
            {
                logger.LogError(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
                return;
            }

            if (!npcDialogService.IsOpenWith(clientSession.Character, aliveEntity))
            {
                return;
            }

            if (shoppingPacket.VisualType == VisualType.Npc && shoppingPacket.ShopType == FamilyShopService.ShopType)
            {
                npcDialogService.Open(clientSession.Character, aliveEntity, NpcDialogWindow.Shop);
                await familyShopService.OpenAsync(clientSession, aliveEntity);
                return;
            }

            if (aliveEntity.Shop?.ShopItems.IsEmpty == false)
            {
                npcDialogService.Open(clientSession.Character, aliveEntity, NpcDialogWindow.Shop);
                await clientSession.SendPacketAsync(aliveEntity.GenerateNInv(percent, shoppingPacket.ShopType));
            }
        }
    }
}
