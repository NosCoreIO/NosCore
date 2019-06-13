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
using NosCore.Data.Enumerations.Bazaar;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.PacketHandlers.CharacterScreen
{
    public class CSListPacketHandler : PacketHandler<CSListPacket>, IWorldPacketHandler
    {
        private readonly IWebApiAccess _webApiAccess;

        public CSListPacketHandler(IWebApiAccess webApiAccess)
        {
            _webApiAccess = webApiAccess;
        }

        public override void Execute(CSListPacket packet, ClientSession clientSession)
        {
            var list = new List<RcsListPacket.RcsListElementPacket>();
            var bzlist = _webApiAccess.Get<List<BazaarLink>>(WebApiRoute.Bazaar, $"-1&Index={packet.Index}&pageSize=50&TypeFilter=0&SubTypeFilter=0&LevelFilter=0&RareFilter=0&UpgradeFilter=0&sellerFilter={clientSession.Character.CharacterId}") ?? new List<BazaarLink>();

            foreach (var bz in bzlist)
            {
                int soldedAmount = bz.BazaarItem.Amount - bz.ItemInstance.Amount;
                int amount = bz.BazaarItem.Amount;
                bool isNosbazar = bz.BazaarItem.MedalUsed;
                long price = bz.BazaarItem.Price;
                long minutesLeft = (long)(bz.BazaarItem.DateStart.AddHours(bz.BazaarItem.Duration) - DateTime.Now).TotalMinutes;
                var status = minutesLeft >= 0 ? (soldedAmount < amount ? BazaarStatusType.OnSale : BazaarStatusType.Solded) : BazaarStatusType.DelayExpired;
                if (status == BazaarStatusType.DelayExpired)
                {
                    minutesLeft = (long)(bz.BazaarItem.DateStart.AddHours(bz.BazaarItem.Duration).AddDays(isNosbazar ? 30 : 7) - DateTime.Now).TotalMinutes;
                }

                var info = new EInfoPacket();

                if (packet.Filter == BazaarStatusType.Default || packet.Filter == status)
                {
                    list.Add(new RcsListPacket.RcsListElementPacket()
                    {
                        AuctionId = bz.BazaarItem.BazaarItemId,
                        OwnerId = bz.BazaarItem.SellerId,
                        ItemId = bz.ItemInstance.ItemVNum,
                        SoldAmount = soldedAmount,
                        Amount = amount,
                        IsPackage = bz.BazaarItem.IsPackage,
                        Status = status,
                        Price = price,
                        MinutesLeft = minutesLeft,
                        IsSellerUsingMedal = isNosbazar,
                        Unknown = 0,
                        Rarity = bz.ItemInstance.Rare,
                        Upgrade = bz.ItemInstance.Upgrade,
                        EInfo = info
                    });
                }
            }

            clientSession.SendPacket(new RcsListPacket
            {
                PageNumber = packet.Index,
                Items = list
            });
        }
    }
}