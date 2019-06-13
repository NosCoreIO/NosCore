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
using ChickenAPI.Packets.ServerPackets.Auction;
using ChickenAPI.Packets.ServerPackets.Inventory;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider.Item;
using static ChickenAPI.Packets.ServerPackets.Auction.RcbListPacket;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CBListPacketHandler : PacketHandler<CBListPacket>, IWorldPacketHandler
    {
        private readonly IWebApiAccess _webApiAccess;
        private readonly List<ItemDto> _items;

        public CBListPacketHandler(IWebApiAccess webApiAccess, List<ItemDto> items)
        {
            _webApiAccess = webApiAccess;
            _items = items;
        }

        public override void Execute(CBListPacket packet, ClientSession clientSession)
        {
            var itemssearch = packet.ItemVNumFilter.FirstOrDefault() == 0 ? new List<short>() : packet.ItemVNumFilter;

            var bzlist = _webApiAccess.Get<List<BazaarLink>>(WebApiRoute.Bazaar, $"-1&Index={packet.Index}&PageSize=50&TypeFilter={packet.TypeFilter}&SubTypeFilter={packet.SubTypeFilter}&LevelFilter={packet.LevelFilter}&RareFilter={packet.RareFilter}&UpgradeFilter={packet.UpgradeFilter}&SellerFilter={null}") ?? new List<BazaarLink>();
            var bzlistsearched = bzlist.Where(s => itemssearch.Contains(s.ItemInstance.ItemVNum)).ToList();

            //price up price down quantity up quantity down
            var definitivelist = itemssearch.Any() ? bzlistsearched : bzlist;
            switch (packet.OrderFilter)
            {
                case 0:
                    definitivelist = definitivelist.OrderBy(s => _items.First(o => o.VNum == s.ItemInstance.ItemVNum).Name).ThenBy(s => s.BazaarItem.Price).ToList();
                    break;

                case 1:
                    definitivelist = definitivelist.OrderBy(s => _items.First(o => o.VNum == s.ItemInstance.ItemVNum).Name).ThenByDescending(s => s.BazaarItem.Price).ToList();
                    break;

                case 2:
                    definitivelist = definitivelist.OrderBy(s => _items.First(o => o.VNum == s.ItemInstance.ItemVNum).Name).ThenBy(s => s.BazaarItem.Amount).ToList();
                    break;

                case 3:
                    definitivelist = definitivelist.OrderBy(s => _items.First(o=>o.VNum == s.ItemInstance.ItemVNum).Name).ThenByDescending(s => s.BazaarItem.Amount).ToList();
                    break;

                default:
                    definitivelist = definitivelist.OrderBy(s => _items.First(o => o.VNum == s.ItemInstance.ItemVNum).Name).ToList();
                    break;
            }

            clientSession.SendPacket(new RcbListPacket
            {
                PageIndex = packet.Index,
                Items = definitivelist.Where(s => (s.BazaarItem.DateStart.AddHours(s.BazaarItem.Duration) - DateTime.Now).TotalMinutes > 0 && s.ItemInstance.Amount > 0).Select(bzlink => new RcbListElementPacket
                {
                    AuctionId = bzlink.BazaarItem.BazaarItemId,
                    OwnerId = bzlink.BazaarItem.SellerId,
                    OwnerName = bzlink.SellerName,
                    ItemId = bzlink.ItemInstance.ItemVNum,
                    Amount = bzlink.ItemInstance.Amount,
                    IsPackage = bzlink.BazaarItem.IsPackage,
                    Price = bzlink.BazaarItem.Price,
                    MinutesLeft = (long)(bzlink.BazaarItem.DateStart.AddHours(bzlink.BazaarItem.Duration) - DateTime.Now).TotalMinutes,
                    Unknown1 = false,
                    Unknown = 2,
                    Rarity = bzlink.ItemInstance.Rare,
                    Upgrade = bzlink.ItemInstance.Upgrade,
                    EInfo = new EInfoPacket(),
                }).ToList()
            });
        }
    }
}