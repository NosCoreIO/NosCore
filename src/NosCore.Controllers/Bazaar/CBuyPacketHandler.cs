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

using ChickenAPI.Packets.ClientPackets.Bazaar;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Bazaar;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using Serilog;
using System;
using System.Collections.Generic;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CBuyPacketHandler : PacketHandler<CBuyPacket>, IWorldPacketHandler
    {
        private readonly IBazaarHttpClient _bazaarHttpClient;
        private readonly IItemProvider _itemProvider;
        private readonly ILogger _logger;

        public CBuyPacketHandler(IBazaarHttpClient bazaarHttpClient, IItemProvider itemProvider, ILogger logger)
        {
            _bazaarHttpClient = bazaarHttpClient;
            _itemProvider = itemProvider;
            _logger = logger;
        }

        public override void Execute(CBuyPacket packet, ClientSession clientSession)
        {
            if (clientSession.Character.InExchangeOrTrade)
            {
                return;
            }
            var bz = _bazaarHttpClient.GetBazaarLink(packet.BazaarId);
            if (bz != null && bz.SellerName != clientSession.Character.Name && packet.Price == bz.BazaarItem.Price)
            {
                var price = bz.BazaarItem.Price * packet.Amount;
                if (clientSession.Character.Inventory.CanAddItem(bz.ItemInstance.ItemVNum) && bz.ItemInstance.Amount >= packet.Amount)
                {
                    if (clientSession.Character.Gold - price > 0)
                    {
                        clientSession.Character.Gold -= price;
                        clientSession.SendPacket(clientSession.Character.GenerateGold());

                        var item = _itemProvider.Convert(bz.ItemInstance);
                        item.Id = Guid.NewGuid();
                        var newInv = clientSession.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(item, clientSession.Character.CharacterId));
                        clientSession.SendPacket(newInv.GeneratePocketChange());

                        var remove = _bazaarHttpClient.Remove(packet.BazaarId, packet.Amount, clientSession.Character.Name);
                        if (remove)
                        {
                            clientSession.HandlePackets(new[] { new CBListPacket { Index = 0, ItemVNumFilter = new List<short>() } });
                            clientSession.SendPacket(new RCBuyPacket
                            {
                                Type = VisualType.Player,
                                VNum = bz.ItemInstance.ItemVNum,
                                Owner = bz.BazaarItem.SellerId,
                                Amount = packet.Amount,
                                Price = packet.Price,
                                Unknown1 = 0,
                                Unknown2 = 0,
                                Unknown3 = 0,
                            });
                            clientSession.SendPacket(clientSession.Character.GenerateSay(
                                $"{Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, clientSession.Account.Language)}: {item.Item.Name[clientSession.Account.Language]} x {packet.Amount}"
                                , SayColorType.Yellow
                            ));

                            return;
                        }
                        else
                        {
                            _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.BAZAAR_BUY_ERROR));
                        }
                    }
                    else
                    {
                        clientSession.SendPacket(clientSession.Character.GenerateSay(
                            Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, clientSession.Account.Language), SayColorType.Yellow
                        ));
                        clientSession.SendPacket(new ModalPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, clientSession.Account.Language),
                            Type = 1
                        });
                        return;
                    }
                }
                else
                {
                    clientSession.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                            clientSession.Account.Language)
                    });
                    return;
                }
            }
            clientSession.SendPacket(new ModalPacket
            {
                Message = Language.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, clientSession.Account.Language),
                Type = 1
            });
        }
    }
}