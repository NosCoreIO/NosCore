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
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.ServerPackets.Auction;
using NosCore.Packets.ServerPackets.Inventory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using static NosCore.Packets.ServerPackets.Auction.RcbListPacket;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CBListPacketHandler : PacketHandler<CBListPacket>, IWorldPacketHandler
    {
        private readonly IBazaarHttpClient _bazaarHttpClient;
        private readonly List<ItemDto> _items;
        private readonly IClock _clock;

        public CBListPacketHandler(IBazaarHttpClient bazaarHttpClient, List<ItemDto> items, IClock clock)
        {
            _bazaarHttpClient = bazaarHttpClient;
            _items = items;
            _clock = clock;
        }

        public override async Task ExecuteAsync(CBListPacket packet, ClientSession clientSession)
        {
            var itemssearch = packet.ItemVNumFilter?.FirstOrDefault() == 0 ? new List<short>() : packet.ItemVNumFilter;

            var bzlist = await _bazaarHttpClient.GetBazaarLinksAsync(-1, packet.Index, 50, packet.TypeFilter, packet.SubTypeFilter,
                packet.LevelFilter, packet.RareFilter, packet.UpgradeFilter, null).ConfigureAwait(false);
            var bzlistsearched = bzlist.Where(s => itemssearch!.Contains(s.ItemInstance!.ItemVNum)).ToList();

            //price up price down quantity up quantity down
            var definitivelist = itemssearch?.Any() == true ? bzlistsearched : bzlist;
            definitivelist = packet.OrderFilter switch
            {
                0 => definitivelist
                    .OrderBy(s => _items.First(o => o.VNum == s.ItemInstance!.ItemVNum).Name[clientSession.Account.Language])
                    .ThenBy(s => s.BazaarItem!.Price)
                    .ToList(),
                1 => definitivelist
                    .OrderBy(s => _items.First(o => o.VNum == s.ItemInstance!.ItemVNum).Name[clientSession.Account.Language])
                    .ThenByDescending(s => s.BazaarItem!.Price)
                    .ToList(),
                2 => definitivelist
                    .OrderBy(s => _items.First(o => o.VNum == s.ItemInstance!.ItemVNum).Name[clientSession.Account.Language])
                    .ThenBy(s => s.BazaarItem!.Amount)
                    .ToList(),
                3 => definitivelist
                    .OrderBy(s => _items.First(o => o.VNum == s.ItemInstance!.ItemVNum).Name[clientSession.Account.Language])
                    .ThenByDescending(s => s.BazaarItem!.Amount)
                    .ToList(),
                _ => definitivelist.OrderBy(s => _items.First(o => o.VNum == s.ItemInstance!.ItemVNum).Name[clientSession.Account.Language])
                    .ToList()
            };

            await clientSession.SendPacketAsync(new RcbListPacket
            {
                PageIndex = packet.Index,
                Items = definitivelist
                    .Where(s => ((s.BazaarItem!.DateStart.Plus(Duration.FromHours(s.BazaarItem.Duration))
                         > _clock.GetCurrentInstant()) && (s.ItemInstance!.Amount > 0)))
                    .Select(bzlink => new RcbListElementPacket
                    {
                        AuctionId = bzlink.BazaarItem!.BazaarItemId,
                        OwnerId = bzlink.BazaarItem.SellerId,
                        OwnerName = bzlink.SellerName,
                        ItemId = bzlink.ItemInstance!.ItemVNum,
                        Amount = bzlink.ItemInstance.Amount,
                        IsPackage = bzlink.BazaarItem.IsPackage,
                        Price = bzlink.BazaarItem.Price,
                        MinutesLeft =
                            (long)(bzlink.BazaarItem.DateStart.Plus(Duration.FromHours(bzlink.BazaarItem.Duration)).Minus(_clock.GetCurrentInstant()))
                            .TotalMinutes,
                        Unknown1 = false,
                        Unknown = 2,
                        Rarity = bzlink.ItemInstance.Rare,
                        Upgrade = bzlink.ItemInstance.Upgrade,
                        EInfo = new EInfoPacket()
                    }).ToList() as List<RcbListElementPacket?>
            }).ConfigureAwait(false);
        }
    }
}