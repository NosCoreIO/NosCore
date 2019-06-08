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

        public CBListPacketHandler(IWebApiAccess webApiAccess)
        {
            _webApiAccess = webApiAccess;
        }

        public override void Execute(CBListPacket packet, ClientSession clientSession)
        {
            var itemssearch = packet.ItemVNumFilter == "0" ? new List<string>() : packet.ItemVNumFilter.Split(' ').ToList();

            var result = _webApiAccess.Get<List<BazaarItemDto>>(WebApiRoute.Bazaar, $"{ packet.Index}&TypeFilter={packet.TypeFilter}&SubTypeFilter={packet.SubTypeFilter}&LevelFilter={packet.LevelFilter}&RareFilter={packet.RareFilter}&UpgradeFilter={packet.UpgradeFilter}") ?? new List<BazaarItemDto>();
            var bzlist = new List<BazaarItem>();
         
            var bzlistsearched = bzlist.Where(s => itemssearch.Contains(s.ItemInstance.ItemVNum.ToString())).ToList();

            //price up price down quantity up quantity down
            var definitivelist = itemssearch.Any() ? bzlistsearched : bzlist;
            switch (packet.OrderFilter)
            {
                case 0:
                    definitivelist = definitivelist.OrderBy(s => s.ItemInstance.Item.Name).ThenBy(s => s.Price).ToList();
                    break;

                case 1:
                    definitivelist = definitivelist.OrderBy(s => s.ItemInstance.Item.Name).ThenByDescending(s => s.Price).ToList();
                    break;

                case 2:
                    definitivelist = definitivelist.OrderBy(s => s.ItemInstance.Item.Name).ThenBy(s => s.Amount).ToList();
                    break;

                case 3:
                    definitivelist = definitivelist.OrderBy(s => s.ItemInstance.Item.Name).ThenByDescending(s => s.Amount).ToList();
                    break;

                default:
                    definitivelist = definitivelist.OrderBy(s => s.ItemInstance.Item.Name).ToList();
                    break;
            }

            clientSession.SendPacket(new RcbListPacket
            {
                PageIndex = packet.Index,
                Items = definitivelist.Where(s => (s.DateStart.AddHours(s.Duration) - DateTime.Now).TotalMinutes > 0 && s.ItemInstance.Amount > 0).Skip(packet.Index * 50).Take(50).Select(bzlink => new RcbListElementPacket
                {
                    AuctionId = bzlink.BazaarItemId,
                    OwnerId = bzlink.SellerId,
                    OwnerName = bzlink.SellerName,
                    ItemId = bzlink.ItemInstance.Item.VNum,
                    Amount = bzlink.ItemInstance.Amount,
                    IsPackage = bzlink.IsPackage,
                    Price = bzlink.Price,
                    MinutesLeft = (long)(bzlink.DateStart.AddHours(bzlink.Duration) - DateTime.Now).TotalMinutes,
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