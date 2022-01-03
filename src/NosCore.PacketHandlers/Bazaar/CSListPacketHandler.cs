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

using NosCore.Core;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Auction;
using NosCore.Packets.ServerPackets.Inventory;
using System.Collections.Generic;
using System.Threading.Tasks;
using NodaTime;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CSListPacketHandler : PacketHandler<CSListPacket>, IWorldPacketHandler
    {
        private readonly IBazaarHttpClient _bazaarHttpClient;
        private readonly IClock _clock;

        public CSListPacketHandler(IBazaarHttpClient bazaarHttpClient, IClock clock)
        {
            _bazaarHttpClient = bazaarHttpClient;
            _clock = clock;
        }

        public override async Task ExecuteAsync(CSListPacket packet, ClientSession clientSession)
        {
            var list = new List<RcsListPacket.RcsListElementPacket?>();
            var bzlist = await _bazaarHttpClient.GetBazaarLinksAsync(-1, packet.Index, 50, 0, 0, 0, 0, 0,
                clientSession.Character.CharacterId).ConfigureAwait(false);

            foreach (var bz in bzlist)
            {
                var soldedAmount = bz.BazaarItem!.Amount - bz.ItemInstance!.Amount;
                int amount = bz.BazaarItem.Amount;
                var isNosbazar = bz.BazaarItem.MedalUsed;
                var price = bz.BazaarItem.Price;
                var minutesLeft = (long)(bz.BazaarItem.DateStart.Plus(Duration.FromHours(bz.BazaarItem.Duration)) - _clock.GetCurrentInstant())
                    .TotalMinutes;
                var status = minutesLeft >= 0 ? soldedAmount < amount ? BazaarStatusType.OnSale
                    : BazaarStatusType.Solded : BazaarStatusType.DelayExpired;
                if (status == BazaarStatusType.DelayExpired)
                {
                    minutesLeft =
                        (long)(bz.BazaarItem.DateStart.Plus(Duration.FromHours(bz.BazaarItem.Duration)).Plus(Duration.FromDays(isNosbazar ? 30 : 7)) -
                            _clock.GetCurrentInstant()).TotalMinutes;
                }

                var info = new EInfoPacket();

                if ((packet.Filter == BazaarStatusType.Default) || (packet.Filter == status))
                {
                    list.Add(new RcsListPacket.RcsListElementPacket
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

            await clientSession.SendPacketAsync(new RcsListPacket
            {
                PageNumber = packet.Index,
                Items = list
            }).ConfigureAwait(false);
        }
    }
}