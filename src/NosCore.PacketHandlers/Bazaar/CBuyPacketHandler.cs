//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
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
    public class CBuyPacketHandler(IBazaarHub bazaarHttpClient, IItemGenerationService itemProvider,
            ILogger<CBuyPacketHandler> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISaveService saveService,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao,
            INpcDialogService npcDialogService, IShellOptionService shellOptionService)
        : PacketHandler<CBuyPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CBuyPacket packet, ClientSession clientSession)
        {
            if (!npcDialogService.IsOpen(clientSession.Character, NpcDialogWindow.Bazaar))
            {
                return;
            }

            if (packet.Amount <= 0)
            {
                return;
            }

            var bzs = await bazaarHttpClient.GetBazaar(packet.BazaarId, null, null, null, null, null, null, null, null);
            var bz = bzs.FirstOrDefault();
            if ((bz != null) && (bz.BazaarItem?.SellerId != clientSession.Character.CharacterId) &&
                (packet.Price == bz.BazaarItem?.Price) && (bz.ItemInstance?.Amount >= packet.Amount))
            {
                if (bz.BazaarItem.IsPackage && (bz.BazaarItem.Amount != packet.Amount))
                {
                    return;
                }

                if (packet.Price > long.MaxValue / packet.Amount)
                {
                    return;
                }

                var price = bz.BazaarItem.Price * packet.Amount;
                if (clientSession.Character.InventoryService.CanAddItem(bz.ItemInstance.ItemVNum))
                {
                    if (clientSession.Character.Gold >= price)
                    {
                        // The bought amount is delivered from a copy taken now: once claimed, the
                        // listing only holds what is left, or nothing at all. The copy is read from the
                        // database because the listing crossed the bus as the base item type.
                        var listedId = bz.ItemInstance.Id;
                        var listed = await itemInstanceDao.FirstOrDefaultAsync(s => s!.Id == listedId);
                        if (listed == null)
                        {
                            await clientSession.SendPacketAsync(new ModaliPacket
                            {
                                Type = 1,
                                Message = Game18NConstString.OfferUpdated
                            });
                            return;
                        }

                        var item = itemProvider.Convert(listed);
                        item.Id = Guid.NewGuid();
                        item.Amount = packet.Amount;

                        var operationId = Guid.NewGuid();
                        bool claimed;
                        try
                        {
                            claimed = await BazaarHubOperations.RequestAsync(() => bazaarHttpClient.BuyBazaarAsync(
                                operationId, packet.BazaarId, packet.Amount, packet.Price, clientSession.Character.CharacterId));
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, logLanguage[LogLanguageKey.BAZAAR_BUY_ERROR]);
                            await bazaarHttpClient.CompensateAsync(operationId, logger, logLanguage);
                            claimed = false;
                        }

                        if (claimed)
                        {
                            List<InventoryItemInstance>? newInv = null;
                            if (BazaarHubOperations.CanApply(clientSession) && (clientSession.Character.Gold >= price))
                            {
                                newInv = clientSession.Character.InventoryService.AddItemToPocket(
                                    InventoryItemInstance.Create(item, clientSession.Character.CharacterId));
                            }

                            if (newInv is not { Count: > 0 })
                            {
                                await bazaarHttpClient.CompensateAsync(operationId, logger, logLanguage);
                                if (BazaarHubOperations.CanApply(clientSession))
                                {
                                    await clientSession.SendPacketAsync(new InfoiPacket
                                    {
                                        Message = Game18NConstString.NotEnoughSpace
                                    });
                                }

                                return;
                            }

                            clientSession.Character.Gold -= price;
                            if (item is WearableInstance)
                            {
                                await itemInstanceDao.TryInsertOrUpdateAsync(item);
                                await shellOptionService.CopyAsync(listedId, item.Id);
                            }

                            await bazaarHttpClient.SettleAsync(saveService, clientSession, operationId, logger, logLanguage);
                            await clientSession.SendPacketAsync(clientSession.Character.GenerateGold());
                            await clientSession.SendPacketAsync(newInv.GeneratePocketChange());

                            await clientSession.HandlePacketsAsync(new[] { new CBListPacket { Index = 0, ItemVNumFilter = new List<short>() } });
                            await clientSession.SendPacketAsync(new RCBuyPacket(bz.SellerName!)
                            {
                                Type = VisualType.Player,
                                VNum = bz.ItemInstance.ItemVNum,
                                Amount = packet.Amount,
                                Price = packet.Price,
                                Slot = 0, //TODO: Add slot
                                Upgrade = bz.ItemInstance.Upgrade,
                                Rarity = (byte)bz.ItemInstance.Rare
                            });

                            await clientSession.SendPacketAsync(new SayiPacket
                            {
                                VisualType = VisualType.Player,
                                VisualId = clientSession.Character.CharacterId,
                                Type = SayColorType.Yellow,
                                Message = Game18NConstString.BoughtItem,
                                ArgumentType = 2,
                                Game18NArguments = { bz.ItemInstance.ItemVNum.ToString(), packet.Amount }
                            });

                            return;
                        }

                        // Someone else took it first, most likely from another channel.
                        logger.LogInformation(logLanguage[LogLanguageKey.BAZAAR_BUY_ERROR]);
                        await clientSession.SendPacketAsync(new ModaliPacket
                        {
                            Type = 1,
                            Message = Game18NConstString.OfferUpdated
                        });
                        await clientSession.HandlePacketsAsync(new[] { new CBListPacket { Index = 0, ItemVNumFilter = new List<short>() } });
                    }
                    else
                    {
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = clientSession.Character.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.InsufficientGoldAvailable
                        });
                        await clientSession.SendPacketAsync(new ModaliPacket
                        {
                            Type = 1,
                            Message = Game18NConstString.InsufficientGoldAvailable
                        });
                        return;
                    }
                }
                else
                {
                    await clientSession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.NotEnoughSpace
                    });
                    return;
                }
            }

            await clientSession.SendPacketAsync(new ModaliPacket
            {
                Type = 1,
                Message = Game18NConstString.OfferUpdated
            });
        }
    }
}
