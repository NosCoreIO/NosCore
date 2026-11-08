//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SaveService;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Bazaar;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CRegPacketHandler(IOptions<WorldConfiguration> configuration, IBazaarHub bazaarHttpClient,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao, ISaveService saveService, ILogger<CRegPacketHandler> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            INpcDialogService npcDialogService, IShellOptionService shellOptionService)
        : PacketHandler<CRegPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CRegPacket cRegPacket, ClientSession clientSession)
        {
            if (!npcDialogService.IsOpen(clientSession.Character, NpcDialogWindow.Bazaar))
            {
                return;
            }

            if (cRegPacket.Amount <= 0 || cRegPacket.Price < 0)
            {
                return;
            }

            var medal = clientSession.Character.StaticBonusList.FirstOrDefault(s =>
                (s.StaticBonusType == StaticBonusType.BazaarMedalGold) ||
                (s.StaticBonusType == StaticBonusType.BazaarMedalSilver));

            if (cRegPacket.Price > long.MaxValue / cRegPacket.Amount)
            {
                return;
            }

            var price = cRegPacket.Price * cRegPacket.Amount;
            var taxmax = price > 100000 ? price / 200 : 500;
            var taxmin = price >= 4000 ? 60 + (price - 4000) / 2000 * 30 > 10000 ? 10000
                : 60 + (price - 4000) / 2000 * 30 : 50;
            var tax = medal == null ? taxmax : taxmin;
            var maxGold = configuration.Value.MaxGoldAmount;
            if (clientSession.Character.Gold < tax)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughGold
                });
                return;
            }

            var it = clientSession.Character.InventoryService.LoadBySlotAndType(cRegPacket.Slot,
                cRegPacket.Inventory == 4 ? 0 : (NoscorePocketType)cRegPacket.Inventory);
            if ((it?.ItemInstance == null) || !it.ItemInstance.Item.IsSoldable || (it.ItemInstance.BoundCharacterId != null) ||
                (cRegPacket.Amount > it.ItemInstance.Amount))
            {
                return;
            }

            var maxPrice = medal == null ? 1000000 : maxGold;

            if (price > maxPrice)
            {
                await clientSession.SendPacketAsync(new ModaliPacket
                {
                    Type = 1,
                    Message = Game18NConstString.NotExceedMaxPrice,
                    ArgumentType = 4,
                    Game18NArguments = { maxPrice }
                });
                return;
            }

            if ((medal == null) && (cRegPacket.Durability > BazaarListingDuration.OneDay))
            {
                return;
            }

            short duration;
            switch (cRegPacket.Durability)
            {
                case BazaarListingDuration.OneDay:
                    duration = 24;
                    break;

                case BazaarListingDuration.SevenDays:
                    duration = 168;
                    break;

                case BazaarListingDuration.FifteenDays:
                    duration = 360;
                    break;

                case BazaarListingDuration.ThirtyDays:
                    duration = 720;
                    break;

                default:
                    return;
            }

            var bazar = clientSession.Character.InventoryService.LoadBySlotAndType(cRegPacket.Slot,
                cRegPacket.Inventory == 4 ? NoscorePocketType.Equipment : (NoscorePocketType)cRegPacket.Inventory);
            if ((bazar?.ItemInstance == null) || (cRegPacket.Amount > bazar.ItemInstance.Amount))
            {
                return;
            }

            // The listing gets its own instance holding the listed amount; the stack in the
            // inventory is only reduced once the master accepted the listing.
            var listed = (IItemInstance)bazar.ItemInstance.Clone();
            listed.Id = Guid.NewGuid();
            listed.Amount = cRegPacket.Amount;
            if (await itemInstanceDao.TryInsertOrUpdateAsync(listed) == null)
            {
                return;
            }

            if (listed is WearableInstance)
            {
                await shellOptionService.CopyAsync(bazar.ItemInstance.Id, listed.Id);
            }

            var operationId = Guid.NewGuid();
            LanguageKey result;
            try
            {
                result = await BazaarHubOperations.RequestAsync(() => bazaarHttpClient.AddBazaarAsync(new BazaarRequest
                {
                    OperationId = operationId,
                    ItemInstanceId = listed.Id,
                    CharacterId = clientSession.Character.CharacterId,
                    CharacterName = clientSession.Character.Name,
                    HasMedal = medal != null,
                    Price = cRegPacket.Price,
                    IsPackage = cRegPacket.IsPackage,
                    Duration = duration,
                    Amount = cRegPacket.Amount
                }));
            }
            catch (Exception e)
            {
                logger.LogError(e, logLanguage[LogLanguageKey.BAZAAR_ADD_ERROR]);
                await bazaarHttpClient.CompensateAsync(operationId, logger, logLanguage);
                await itemInstanceDao.TryDeleteAsync(listed.Id);
                return;
            }

            switch (result)
            {
                case LanguageKey.LIMIT_EXCEEDED:
                    await itemInstanceDao.TryDeleteAsync(listed.Id);
                    await clientSession.SendPacketAsync(new ModaliPacket
                    {
                        Type = 1,
                        Message = Game18NConstString.ListedMaxItemsNumber
                    });
                    break;

                case LanguageKey.OBJECT_IN_BAZAAR:
                    if (!BazaarHubOperations.CanApply(clientSession)
                        || !clientSession.Character.InventoryService.TryGetValue(bazar.ItemInstanceId, out var stack)
                        || (stack.ItemInstance.Amount < cRegPacket.Amount) || (clientSession.Character.Gold < tax))
                    {
                        await bazaarHttpClient.CompensateAsync(operationId, logger, logLanguage);
                        return;
                    }

                    if (stack.ItemInstance.Amount == cRegPacket.Amount)
                    {
                        clientSession.Character.InventoryService.Remove(stack.ItemInstanceId);
                    }
                    else
                    {
                        stack.ItemInstance.Amount -= cRegPacket.Amount;
                    }

                    clientSession.Character.Gold -= tax;
                    await bazaarHttpClient.SettleAsync(saveService, clientSession, operationId, logger, logLanguage);

                    await clientSession.SendPacketAsync(((InventoryItemInstance?)null).GeneratePocketChange(
                        cRegPacket.Inventory == 4 ? PocketType.Equipment : (PocketType)cRegPacket.Inventory,
                        cRegPacket.Slot));
                    await clientSession.SendPacketAsync(clientSession.Character.GenerateGold());
                    await clientSession.SendPacketAsync(new SayiPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = clientSession.Character.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.ItemAddedToBazar
                    });
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.ItemAddedToBazar
                    });

                    await clientSession.SendPacketAsync(new RCRegPacket { Type = VisualType.Player });
                    break;
            }
        }
    }
}
