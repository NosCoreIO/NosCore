//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Bazaar;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CBuyPacketHandler(IBazaarHub bazaarHttpClient, IItemGenerationService itemProvider,
            ILogger logger,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<CBuyPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CBuyPacket packet, ClientSession clientSession)
        {
            if (packet.Amount <= 0)
            {
                return;
            }

            var bzs = await bazaarHttpClient.GetBazaar(packet.BazaarId, null, null, null, null, null, null, null, null);
            var bz = bzs.FirstOrDefault();
            if ((bz != null) && (bz.SellerName != clientSession.Character.Name) &&
                (packet.Price == bz.BazaarItem?.Price) && (bz.ItemInstance?.Amount >= packet.Amount))
            {
                if (bz.BazaarItem.IsPackage && (bz.BazaarItem.Amount != packet.Amount))
                {
                    return;
                }

                var price = bz.BazaarItem.Price * packet.Amount;
                if (clientSession.Character.InventoryService.CanAddItem(bz.ItemInstance.ItemVNum))
                {
                    if (clientSession.Character.Gold >= price)
                    {
                        clientSession.Character.Gold -= price;
                        await clientSession.SendPacketAsync(clientSession.Character.GenerateGold());

                        var itemInstance = await itemInstanceDao.FirstOrDefaultAsync(s => s!.Id == bz.ItemInstance.Id);
                        var item = itemProvider.Convert(itemInstance!);
                        item.Id = Guid.NewGuid();
                        var newInv =
                            clientSession.Character.InventoryService.AddItemToPocket(
                                InventoryItemInstance.Create(item, clientSession.Character.CharacterId));
                        await clientSession.SendPacketAsync(newInv!.GeneratePocketChange());

                        var remove = await bazaarHttpClient.DeleteBazaarAsync(packet.BazaarId, packet.Amount,
                            clientSession.Character.Name);
                        if (remove)
                        {
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

                        logger.Error(logLanguage[LogLanguageKey.BAZAAR_BUY_ERROR]);
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
