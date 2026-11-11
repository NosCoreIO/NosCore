//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.SaveService;
using NosCore.GameObject.Services.ShellOptionService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Bazaar;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CScalcPacketHandler(IOptions<WorldConfiguration> worldConfiguration,
            IBazaarHub bazaarHttpClient,
            IItemGenerationService itemProvider, ILogger<CScalcPacketHandler> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISaveService saveService,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            INpcDialogService npcDialogService, IShellOptionService shellOptionService)
        : PacketHandler<CScalcPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CScalcPacket packet, ClientSession clientSession)
        {
            if (!npcDialogService.IsOpen(clientSession.Character, NpcDialogWindow.Bazaar))
            {
                return;
            }

            var bzs = await bazaarHttpClient.GetBazaar(packet.BazaarId, null, null, null, null, null, null, null, null);
            var bz = bzs.FirstOrDefault();
            if ((bz?.BazaarItem == null) || (bz.ItemInstance == null) ||
                (bz.BazaarItem.SellerId != clientSession.Character.CharacterId))
            {
                await clientSession.SendPacketAsync(new RCScalcPacket
                { Type = VisualType.Player, Price = 0, RemainingAmount = 0, Amount = 0, Taxes = 0, Total = 0 });
                return;
            }

            // Sales can still land until the claim, so the checks assume the best case for gold
            // and the worst case for space.
            if ((bz.ItemInstance.Amount > 0) &&
                !clientSession.Character.InventoryService.CanAddItem(bz.ItemInstance.ItemVNum))
            {
                await clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.NotEnoughSpace
                });
                await SendFailureAsync(clientSession, bz.BazaarItem);
                return;
            }

            if (clientSession.Character.Gold + Payout(bz.BazaarItem, bz.BazaarItem.Amount).Price >
                worldConfiguration.Value.MaxGoldAmount)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.MaxGoldReached
                });
                await SendFailureAsync(clientSession, bz.BazaarItem);
                return;
            }

            var operationId = Guid.NewGuid();
            BazaarClaim? claim;
            try
            {
                claim = await BazaarHubOperations.RequestAsync(() =>
                    bazaarHttpClient.ClaimBazaarAsync(operationId, packet.BazaarId, clientSession.Character.CharacterId));
            }
            catch (Exception e)
            {
                logger.LogError(e, logLanguage[LogLanguageKey.BAZAAR_DELETE_ERROR]);
                await bazaarHttpClient.CompensateAsync(operationId, logger, logLanguage);
                claim = null;
            }

            if (claim == null)
            {
                await SendFailureAsync(clientSession, bz.BazaarItem);
                return;
            }

            List<InventoryItemInstance>? newInv = null;
            var applicable = BazaarHubOperations.CanApply(clientSession)
                && (clientSession.Character.Gold + claim.Gold <= worldConfiguration.Value.MaxGoldAmount);
            if (applicable && (claim.Item != null))
            {
                // Read back from the database: the claim crossed the bus as the base item type.
                var listedId = claim.Item.Id;
                var listed = await itemInstanceDao.FirstOrDefaultAsync(s => s.Id == listedId);
                var item = itemProvider.Convert(listed ?? claim.Item);
                item.Id = Guid.NewGuid();
                item.Amount = claim.ReturnedAmount;
                applicable = await BazaarHubOperations.CarryOptionsAsync(shellOptionService, itemInstanceDao, item, listedId);
                if (applicable)
                {
                    newInv = clientSession.Character.InventoryService.AddItemToPocket(
                        InventoryItemInstance.Create(item, clientSession.Character.CharacterId));
                    applicable = newInv is { Count: > 0 };
                    if (!applicable)
                    {
                        await BazaarHubOperations.DropAsync(itemInstanceDao, item);
                    }
                }
            }

            if (!applicable)
            {
                await bazaarHttpClient.CompensateAsync(operationId, logger, logLanguage);
                if (BazaarHubOperations.CanApply(clientSession))
                {
                    await SendFailureAsync(clientSession, bz.BazaarItem);
                }

                return;
            }

            clientSession.Character.Gold += claim.Gold;

            await bazaarHttpClient.SettleAsync(saveService, clientSession, operationId, logger, logLanguage);

            var listing = claim.Listing;
            await clientSession.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = clientSession.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.PurchaseCompleted,
                ArgumentType = 2,
                Game18NArguments = { bz.ItemInstance.ItemVNum.ToString(), claim.ReturnedAmount }
            });
            await clientSession.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = clientSession.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.PurchaseCompletedWithGoldUsed,
                ArgumentType = 4,
                Game18NArguments = { claim.Gold }
            });
            await clientSession.SendPacketAsync(clientSession.Character.GenerateGold());
            if (newInv != null)
            {
                await clientSession.SendPacketAsync(newInv.GeneratePocketChange());
            }

            await clientSession.SendPacketAsync(new RCScalcPacket
            {
                Type = VisualType.Player,
                Price = listing.Price,
                RemainingAmount = claim.SoldAmount,
                Amount = listing.Amount,
                Taxes = (short)claim.Taxes,
                Total = claim.Gold + claim.Taxes
            });
            await clientSession.HandlePacketsAsync(new[]
                {new CSListPacket {Index = 0, Filter = BazaarStatusType.Default}});
        }

        private static (long Price, long Taxes) Payout(BazaarItemDto listing, short soldAmount)
        {
            var gross = listing.Price * soldAmount;
            var taxes = listing.MedalUsed ? 0 : (long)(gross * 0.10);
            return (gross - taxes, taxes);
        }

        private static Task SendFailureAsync(ClientSession clientSession, BazaarItemDto listing) =>
            clientSession.SendPacketAsync(new RCScalcPacket
            {
                Type = VisualType.Player, Price = listing.Price, RemainingAmount = 0,
                Amount = listing.Amount, Taxes = 0, Total = 0
            });
    }
}
