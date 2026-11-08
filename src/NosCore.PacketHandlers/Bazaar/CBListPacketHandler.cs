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
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.ServerPackets.Auction;
using NosCore.Packets.ServerPackets.Inventory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static NosCore.Packets.ServerPackets.Auction.RcbListPacket;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CBListPacketHandler(IBazaarHub bazaarHttpClient, List<ItemDto> items, IClock clock,
            INpcDialogService npcDialogService)
        : PacketHandler<CBListPacket>, IWorldPacketHandler
    {
        private const int PageSize = 50;

        public override async Task ExecuteAsync(CBListPacket packet, ClientSession clientSession)
        {
            if (!npcDialogService.IsOpen(clientSession.Character, NpcDialogWindow.Bazaar))
            {
                return;
            }

            var vnumFilter = packet.ItemVNumFilter?.Where(s => s != 0).ToHashSet() ?? new HashSet<short>();

            var bzlist = await bazaarHttpClient.GetBazaar(-1, null, null, packet.TypeFilter, packet.SubTypeFilter,
                packet.LevelFilter, packet.RareFilter, packet.UpgradeFilter, null);
            var now = clock.GetCurrentInstant();
            var searched = bzlist.Where(s => (vnumFilter.Count == 0 || vnumFilter.Contains(s.ItemInstance!.ItemVNum))
                && s.BazaarItem!.DateStart.Plus(Duration.FromHours(s.BazaarItem.Duration)) > now
                && s.ItemInstance!.Amount > 0);

            var byName = searched.OrderBy(s => items.First(o => o.VNum == s.ItemInstance!.ItemVNum).Name[clientSession.Account.Language]);
            var definitivelist = (packet.OrderFilter switch
                {
                    0 => byName.ThenBy(s => s.BazaarItem!.Price),
                    1 => byName.ThenByDescending(s => s.BazaarItem!.Price),
                    2 => byName.ThenBy(s => s.BazaarItem!.Amount),
                    3 => byName.ThenByDescending(s => s.BazaarItem!.Amount),
                    _ => byName
                })
                .Skip(packet.Index * PageSize)
                .Take(PageSize)
                .ToList();

            await clientSession.SendPacketAsync(new RcbListPacket
            {
                PageIndex = packet.Index,
                Items = definitivelist
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
                            (long)(bzlink.BazaarItem.DateStart.Plus(Duration.FromHours(bzlink.BazaarItem.Duration)).Minus(now))
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
