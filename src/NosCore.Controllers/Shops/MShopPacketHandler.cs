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
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.PathFinder;

namespace NosCore.PacketHandlers.Shops
{
    public class MShopPacketHandler : PacketHandler<MShopPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(MShopPacket mShopPacket, ClientSession clientSession)
        {
            var portal = clientSession.Character.MapInstance.Portals.Find(port =>
                Heuristic.Octile(Math.Abs(clientSession.Character.PositionX - port.SourceX),
                    Math.Abs(clientSession.Character.PositionY - port.SourceY)) <= 6);
            if (portal != null)
            {
                await clientSession.SendPacketAsync(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.SHOP_NEAR_PORTAL,
                        clientSession.Account.Language),
                    Type = 0
                }).ConfigureAwait(false);
                return;
            }

            if ((clientSession.Character.Group != null) && (clientSession.Character.Group?.Type != GroupType.Group))
            {
                await clientSession.SendPacketAsync(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.SHOP_NOT_ALLOWED_IN_RAID,
                        clientSession.Account.Language),
                    Type = MessageType.White
                }).ConfigureAwait(false);
                return;
            }

            if (!clientSession.Character.MapInstance.ShopAllowed)
            {
                await clientSession.SendPacketAsync(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.SHOP_NOT_ALLOWED,
                        clientSession.Account.Language),
                    Type = MessageType.White
                }).ConfigureAwait(false);
                return;
            }

            switch (mShopPacket.Type)
            {
                case CreateShopPacketType.Open:
                    clientSession.Character.Shop = new Shop();
                    sbyte shopSlot = -1;
                    foreach (var item in mShopPacket.ItemList!)
                    {
                        shopSlot++;
                        if (item!.Amount == 0)
                        {
                            continue;
                        }

                        var inv = clientSession.Character.InventoryService.LoadBySlotAndType(item.Slot,
                            (NoscorePocketType)item.Type);
                        if (inv == null)
                        {
                            //log
                            continue;
                        }

                        if (inv.ItemInstance!.Amount < item.Amount)
                        {
                            //todo log
                            return;
                        }

                        if (!inv.ItemInstance.Item!.IsTradable || (inv.ItemInstance.BoundCharacterId != null))
                        {
                            await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop }).ConfigureAwait(false);
                            await clientSession.SendPacketAsync(clientSession.Character.GenerateSay(
                                GameLanguage.Instance.GetMessageFromKey(LanguageKey.SHOP_ONLY_TRADABLE_ITEMS,
                                    clientSession.Account.Language),
                                SayColorType.Yellow)).ConfigureAwait(false);
                            clientSession.Character.Shop = null;
                            return;
                        }

                        clientSession.Character.Shop.ShopItems.TryAdd(shopSlot,
                            new ShopItem
                            {
                                Amount = item.Amount,
                                Price = item.Price,
                                Slot = (byte)shopSlot,
                                Type = 0,
                                ItemInstance = inv.ItemInstance
                            });
                    }

                    if (clientSession.Character.Shop.ShopItems.Count == 0)
                    {
                        await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop }).ConfigureAwait(false);
                        await clientSession.SendPacketAsync(clientSession.Character.GenerateSay(
                            GameLanguage.Instance.GetMessageFromKey(LanguageKey.SHOP_EMPTY, clientSession.Account.Language),
                            SayColorType.Yellow)).ConfigureAwait(false);
                        clientSession.Character.Shop = null;
                        return;
                    }

                    clientSession.Character.Shop.Session = clientSession;
                    clientSession.Character.Shop.MenuType = 3;
                    clientSession.Character.Shop.ShopId = 501;
                    clientSession.Character.Shop.Size = 60;

                    for (var i = 0; i < clientSession.Character.Shop.Name.Keys.Count; i++)
                    {
                        var key = clientSession.Character.Shop.Name.Keys.ElementAt(i);
                        clientSession.Character.Shop.Name[key] = string.IsNullOrWhiteSpace(mShopPacket.Name) ?
                            GameLanguage.Instance.GetMessageFromKey(LanguageKey.SHOP_PRIVATE_SHOP, key) :
                            mShopPacket.Name.Substring(0, Math.Min(mShopPacket.Name.Length, 20));
                    }

                    await clientSession.Character.MapInstance.SendPacketAsync(clientSession.Character.GenerateShop(clientSession.Character.AccountLanguage)).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(new InfoPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.SHOP_OPEN,
                            clientSession.Account.Language)
                    }).ConfigureAwait(false);

                    clientSession.Character.Requests.Subscribe(data =>
                        data.ClientSession.SendPacketAsync(
                            clientSession.Character.GenerateNpcReq(clientSession.Character.Shop.ShopId)));
                    await clientSession.Character.MapInstance.SendPacketAsync(clientSession.Character.GeneratePFlag(),
                        new EveryoneBut(clientSession.Channel!.Id)).ConfigureAwait(false);
                    clientSession.Character.IsSitting = true;
                    clientSession.Character.LoadSpeed();
                    await clientSession.SendPacketAsync(clientSession.Character.GenerateCond()).ConfigureAwait(false);
                    await clientSession.Character.MapInstance.SendPacketAsync(clientSession.Character.GenerateRest()).ConfigureAwait(false);
                    break;
                case CreateShopPacketType.Close:
                    await clientSession.Character.CloseShopAsync().ConfigureAwait(false);
                    break;
                case CreateShopPacketType.Create:
                    await clientSession.SendPacketAsync(new IshopPacket()).ConfigureAwait(false);
                    break;
                default:
                    //todo log
                    return;
            }
        }
    }
}