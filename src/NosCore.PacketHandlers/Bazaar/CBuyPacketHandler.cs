//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
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
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CBuyPacketHandler(IBazaarHttpClient bazaarHttpClient, IItemGenerationService itemProvider,
            ILogger logger,
            IDao<IItemInstanceDto?, Guid> itemInstanceDao, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<CBuyPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CBuyPacket packet, ClientSession clientSession)
        {
            var bz = await bazaarHttpClient.GetBazaarLinkAsync(packet.BazaarId).ConfigureAwait(false);
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
                    if (clientSession.Character.Gold - price > 0)
                    {
                        clientSession.Character.Gold -= price;
                        await clientSession.SendPacketAsync(clientSession.Character.GenerateGold()).ConfigureAwait(false);

                        var itemInstance = await itemInstanceDao.FirstOrDefaultAsync(s => s!.Id == bz.ItemInstance.Id).ConfigureAwait(false);
                        var item = itemProvider.Convert(itemInstance!);
                        item.Id = Guid.NewGuid();
                        var newInv =
                            clientSession.Character.InventoryService.AddItemToPocket(
                                InventoryItemInstance.Create(item, clientSession.Character.CharacterId));
                        await clientSession.SendPacketAsync(newInv!.GeneratePocketChange()).ConfigureAwait(false);

                        var remove = await bazaarHttpClient.RemoveAsync(packet.BazaarId, packet.Amount,
                            clientSession.Character.Name).ConfigureAwait(false);
                        if (remove)
                        {
                            await clientSession.HandlePacketsAsync(new[] { new CBListPacket { Index = 0, ItemVNumFilter = new List<short>() } }).ConfigureAwait(false);
                            await clientSession.SendPacketAsync(new RCBuyPacket(bz.SellerName!)
                            {
                                Type = VisualType.Player,
                                VNum = bz.ItemInstance.ItemVNum,
                                Amount = packet.Amount,
                                Price = packet.Price,
                                Slot = 0, //TODO: Add slot
                                Upgrade = bz.ItemInstance.Upgrade,
                                Rarity = (byte)bz.ItemInstance.Rare
                            }).ConfigureAwait(false);

                            await clientSession.SendPacketAsync(new SayiPacket
                            {
                                VisualType = VisualType.Player,
                                VisualId = clientSession.Character.CharacterId,
                                Type = SayColorType.Yellow,
                                Message = Game18NConstString.BoughtItem,
                                ArgumentType = 2,
                                Game18NArguments = { bz.ItemInstance.ItemVNum.ToString(), packet.Amount }
                            }).ConfigureAwait(false);

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
                        }).ConfigureAwait(false);
                        await clientSession.SendPacketAsync(new ModaliPacket
                        {
                            Type = 1,
                            Message = Game18NConstString.InsufficientGoldAvailable
                        }).ConfigureAwait(false);
                        return;
                    }
                }
                else
                {
                    await clientSession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.NotEnoughSpace
                    }).ConfigureAwait(false);
                    return;
                }
            }

            await clientSession.SendPacketAsync(new ModaliPacket
            {
                Type = 1,
                Message = Game18NConstString.OfferUpdated
            }).ConfigureAwait(false);
        }
    }
}
