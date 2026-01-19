//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.ServerPackets.Auction;
using NosCore.Packets.ServerPackets.Inventory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static NosCore.Packets.ServerPackets.Auction.RcbListPacket;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CBListPacketHandler(IBazaarHub bazaarHttpClient, List<ItemDto> items, IClock clock)
        : PacketHandler<CBListPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CBListPacket packet, ClientSession clientSession)
        {
            var itemssearch = packet.ItemVNumFilter?.FirstOrDefault() == 0 ? new List<short>() : packet.ItemVNumFilter;

            var bzlist = await bazaarHttpClient.GetBazaar(-1, (byte?)packet.Index, 50, packet.TypeFilter, packet.SubTypeFilter,
                packet.LevelFilter, packet.RareFilter, packet.UpgradeFilter, null);
            var bzlistsearched = bzlist.Where(s => itemssearch!.Contains(s.ItemInstance!.ItemVNum)).ToList();

            //price up price down quantity up quantity down
            var definitivelist = itemssearch?.Any() == true ? bzlistsearched : bzlist;
            definitivelist = packet.OrderFilter switch
            {
                0 => definitivelist
                    .OrderBy(s => items.First(o => o.VNum == s.ItemInstance!.ItemVNum).Name[clientSession.Account.Language])
                    .ThenBy(s => s.BazaarItem!.Price)
                    .ToList(),
                1 => definitivelist
                    .OrderBy(s => items.First(o => o.VNum == s.ItemInstance!.ItemVNum).Name[clientSession.Account.Language])
                    .ThenByDescending(s => s.BazaarItem!.Price)
                    .ToList(),
                2 => definitivelist
                    .OrderBy(s => items.First(o => o.VNum == s.ItemInstance!.ItemVNum).Name[clientSession.Account.Language])
                    .ThenBy(s => s.BazaarItem!.Amount)
                    .ToList(),
                3 => definitivelist
                    .OrderBy(s => items.First(o => o.VNum == s.ItemInstance!.ItemVNum).Name[clientSession.Account.Language])
                    .ThenByDescending(s => s.BazaarItem!.Amount)
                    .ToList(),
                _ => definitivelist.OrderBy(s => items.First(o => o.VNum == s.ItemInstance!.ItemVNum).Name[clientSession.Account.Language])
                    .ToList()
            };

            await clientSession.SendPacketAsync(new RcbListPacket
            {
                PageIndex = packet.Index,
                Items = definitivelist
                    .Where(s => ((s.BazaarItem!.DateStart.Plus(Duration.FromHours(s.BazaarItem.Duration))
                         > clock.GetCurrentInstant()) && (s.ItemInstance!.Amount > 0)))
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
                            (long)(bzlink.BazaarItem.DateStart.Plus(Duration.FromHours(bzlink.BazaarItem.Duration)).Minus(clock.GetCurrentInstant()))
                            .TotalMinutes,
                        Unknown1 = false,
                        Unknown = 2,
                        Rarity = bzlink.ItemInstance.Rare,
                        Upgrade = bzlink.ItemInstance.Upgrade,
                        EInfo = new EInfoPacket()
                    }).ToList() as List<RcbListElementPacket?>
            });
        }
    }
}
