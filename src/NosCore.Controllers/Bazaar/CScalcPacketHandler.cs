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

using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Bazaar;
using ChickenAPI.Packets.ClientPackets.Shops;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Bazaar;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CScalcPacketHandler : PacketHandler<CScalcPacket>, IWorldPacketHandler
    {
        private readonly WorldConfiguration _worldConfiguration;
        private readonly IWebApiAccess _webApiAccess;

        public CScalcPacketHandler(WorldConfiguration worldConfiguration, IWebApiAccess webApiAccess)
        {
            _worldConfiguration = worldConfiguration;
            _webApiAccess = webApiAccess;
        }

        public override void Execute(CScalcPacket packet, ClientSession clientSession)
        {
            if (clientSession.Character.InExchangeOrTrade)
            {
                return;
            }
            var bz = _webApiAccess.Get<List<BazaarLink>>(WebApiRoute.Bazaar, packet.BazaarId).FirstOrDefault();
            if (bz != null && bz.SellerName == clientSession.Character.Name)
            {
                var soldedamount = bz.BazaarItem.Amount - bz.ItemInstance.Amount;
                var taxes = bz.BazaarItem.MedalUsed ? (short)0 : (short)(bz.BazaarItem.Price * 0.10 * soldedamount);
                var price = bz.BazaarItem.Price * soldedamount - taxes;
                if (clientSession.Character.Inventory.CanAddItem(bz.ItemInstance.ItemVNum))
                {
                    if (clientSession.Character.Gold + price <= _worldConfiguration.MaxGoldAmount)
                    {
                        clientSession.Character.Gold += price;
                        clientSession.SendPacket(clientSession.Character.GenerateGold());
                        clientSession.SendPacket(clientSession.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey(LanguageKey.REMOVE_FROM_BAZAAR,
                            clientSession.Account.Language), price), SayColorType.Yellow));

                        if (bz.ItemInstance.Amount != 0)
                        {
                            //var newInv = clientSession.Character.Inventory.AddItemToPocket(InventoryItemInstance.Create(, clientSession.Character.CharacterId));
                        }
                        else
                        {
                            //todo delete
                        }


                        clientSession.SendPacket(new RCScalcPacket { Type = VisualType.Player, Price = bz.BazaarItem.Price, RemainingAmount = (short)(bz.BazaarItem.Amount - bz.ItemInstance.Amount), Amount = bz.BazaarItem.Amount, Taxes = taxes, Total = price + taxes });
                    }
                    else
                    {
                        clientSession.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD,
                                clientSession.Account.Language),
                            Type = MessageType.Whisper
                        });
                    }
                }
                else
                {
                    clientSession.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                            clientSession.Account.Language)
                    });
                }
                clientSession.SendPacket(new RCScalcPacket { Type = VisualType.Player, Price = bz.BazaarItem.Price, RemainingAmount = 0, Amount = bz.BazaarItem.Amount, Taxes = 0, Total = 0 });
            }
            else
            {
                clientSession.SendPacket(new RCScalcPacket { Type = VisualType.Player, Price = 0, RemainingAmount = 0, Amount = 0, Taxes = 0, Total = 0 });
            }
        }
    }
}