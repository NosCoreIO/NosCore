//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Core.I18N;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.GameObject.Services.SkillService;
using NosCore.Algorithm.DignityService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using System;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class BuyPacketHandler(ILogger<BuyPacketHandler> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            ISessionRegistry sessionRegistry, IOptions<WorldConfiguration> worldConfiguration,
            IItemGenerationService itemProvider, INpcDialogService npcDialogService,
            IShellOptionService shellOptionService, IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            IFamilyShopService familyShopService, IDignityService dignityService, ISkillService skillService)
        : PacketHandler<BuyPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(BuyPacket buyPacket, ClientSession clientSession)
        {
            IAliveEntity? aliveEntity;
            switch (buyPacket.VisualType)
            {
                case VisualType.Player:
                    aliveEntity = sessionRegistry.TryGetCharacter(s => s.VisualId == buyPacket.VisualId, out var buyer) ? buyer : null;
                    break;
                case VisualType.Npc:
                    aliveEntity = clientSession.Character.MapInstance.FindNpc(s => s.VisualId == buyPacket.VisualId);
                    break;

                default:
                    logger.LogError(logLanguage[LogLanguageKey.VISUALTYPE_UNKNOWN],
                        buyPacket.VisualType);
                    return Task.CompletedTask;
            }

            if (aliveEntity != null)
            {
                if (aliveEntity.Shop is not { } shop)
                {
                    return Task.CompletedTask;
                }

                if (!npcDialogService.IsOpenWith(clientSession.Character, aliveEntity, NpcDialogWindow.Shop))
                {
                    return Task.CompletedTask;
                }

                if (buyPacket.Amount is not { } amount)
                {
                    var skill = buyPacket.VisualType == VisualType.Npc
                        ? shop.ShopSkills.FirstOrDefault(s => s.SkillVNum == buyPacket.Slot)?.Skill
                        : null;
                    return skill == null ? Task.CompletedTask : skillService.BuyAsync(clientSession.Character, skill);
                }

                return buyPacket.VisualType == VisualType.Npc && shop.ShopType == FamilyShopService.ShopType
                    ? familyShopService.BuyAsync(clientSession, aliveEntity, buyPacket.Slot)
                    : clientSession.Character.BuyAsync(shop, buyPacket.Slot, amount, worldConfiguration,
                        itemProvider, dignityService, shellOptionService, itemInstanceDao);
            }

            logger.LogError(logLanguage[LogLanguageKey.VISUALENTITY_DOES_NOT_EXIST]);
            return Task.CompletedTask;

        }
    }
}
