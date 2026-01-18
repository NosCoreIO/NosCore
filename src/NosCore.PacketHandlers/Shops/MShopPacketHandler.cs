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

using Arch.Core;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.NRunService;
using NosCore.GameObject.Ecs.Systems;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.GameObject.Networking;

namespace NosCore.PacketHandlers.Shops
{
    public class MShopPacketHandler(IHeuristic distanceCalculator, ICondSystem condSystem, IRestSystem restSystem, IEntityPacketSystem entityPacketSystem) : PacketHandler<MShopPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(MShopPacket mShopPacket, ClientSession clientSession)
        {
            var player = clientSession.Player;
            var world = player.MapInstance.EcsWorld;
            var portal = player.MapInstance.Portals.Find(port =>
            {
                var portalData = port.GetPortal(world);
                return portalData != null && distanceCalculator.GetDistance(
                    (player.PositionX, player.PositionY),
                    (portalData.Value.SourceX, portalData.Value.SourceY)) <= 6;
            });

            if (portal != default)
            {
                await clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.OpenShopAwayPortal
                }).ConfigureAwait(false);
                return;
            }

            if ((player.Group != null) && (player.Group?.Type != GroupType.Group))
            {
                await clientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = player.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.TeammateCanNotOpenShop
                }).ConfigureAwait(false);
                return;
            }

            if (!player.MapInstance.ShopAllowed)
            {
                await clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UseCommercialMapToShop
                }).ConfigureAwait(false);
                await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop }).ConfigureAwait(false);
                return;
            }

            switch (mShopPacket.Type)
            {
                case CreateShopPacketType.Open:
                    player.Shop = new Shop();
                    sbyte shopSlot = -1;
                    foreach (var item in mShopPacket.ItemList!)
                    {
                        shopSlot++;
                        if (item!.Amount == 0)
                        {
                            continue;
                        }

                        var inv = player.InventoryService.LoadBySlotAndType(item.Slot,
                            (NoscorePocketType)item.Type);
                        if (inv == null)
                        {
                            continue;
                        }

                        if (inv.ItemInstance.Amount < item.Amount)
                        {
                            return;
                        }

                        if (!inv.ItemInstance.Item.IsTradable || (inv.ItemInstance.BoundCharacterId != null))
                        {
                            await clientSession.SendPacketAsync(new SayiPacket
                            {
                                VisualType = VisualType.Player,
                                VisualId = player.CharacterId,
                                Type = SayColorType.Red,
                                Message = Game18NConstString.SomeItemsCannotBeTraded
                            }).ConfigureAwait(false);
                            await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop }).ConfigureAwait(false);
                            player.Shop = null;
                            return;
                        }

                        player.Shop!.ShopItems.TryAdd(shopSlot,
                            new ShopItem
                            {
                                Amount = item.Amount,
                                Price = item.Price,
                                Slot = (byte)shopSlot,
                                Type = 0,
                                ItemInstance = inv.ItemInstance
                            });
                    }

                    if (player.Shop!.ShopItems.IsEmpty)
                    {
                        await clientSession.SendPacketAsync(new SayiPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = player.CharacterId,
                            Type = SayColorType.Yellow,
                            Message = Game18NConstString.NoItemToSell
                        }).ConfigureAwait(false);
                        await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop }).ConfigureAwait(false);
                        player.Shop = null;
                        return;
                    }

                    player.Shop.OwnerCharacterId = player.CharacterId;
                    player.Shop.MenuType = 3;
                    player.Shop.ShopId = 501;
                    player.Shop.Size = 60;

                    for (var i = 0; i < player.Shop.Name.Keys.Count; i++)
                    {
                        var key = player.Shop.Name.Keys.ElementAt(i);
                        player.Shop.Name[key] = string.IsNullOrWhiteSpace(mShopPacket.Name) ?
                            ((byte)Game18NConstString.PrivateShop).ToString() :
                            mShopPacket.Name.Substring(0, Math.Min(mShopPacket.Name.Length, 20));
                    }

                    await player.MapInstance.SendPacketAsync(entityPacketSystem.GenerateShop(player, player.Shop, player.AccountLanguage)).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.ShopOpened
                    }).ConfigureAwait(false);

                    player.Requests[typeof(INrunEventHandler)].Subscribe(data =>
                        data.ClientSession.SendPacketAsync(
                            entityPacketSystem.GenerateNpcReq(player, player.Shop!.ShopId)));
                    await player.MapInstance.SendPacketAsync(entityPacketSystem.GeneratePFlag(player, player.Shop),
                        new EveryoneBut(clientSession.Channel!.Id)).ConfigureAwait(false);
                    await restSystem.SetRestAsync(player, true);
                    await clientSession.SendPacketAsync(condSystem.GenerateCondPacket(player)).ConfigureAwait(false);
                    break;
                case CreateShopPacketType.Close:
                    if (player.Shop != null)
                    {
                        await player.MapInstance.SendPacketAsync(entityPacketSystem.GenerateShop(player, null, player.AccountLanguage)).ConfigureAwait(false);
                        await player.MapInstance.SendPacketAsync(entityPacketSystem.GeneratePFlag(player, null),
                            new EveryoneBut(clientSession.Channel!.Id)).ConfigureAwait(false);
                    }
                    player.Shop = null;
                    player.InShop = false;
                    await restSystem.SetRestAsync(player, false);
                    await clientSession.SendPacketAsync(condSystem.GenerateCondPacket(player)).ConfigureAwait(false);
                    await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.PersonalShop }).ConfigureAwait(false);
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