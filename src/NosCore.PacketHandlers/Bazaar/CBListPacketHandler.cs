//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.WebApi;
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
    public class CBListPacketHandler(IBazaarHub bazaarHttpClient, IClock clock, INpcDialogService npcDialogService)
        : PacketHandler<CBListPacket>, IWorldPacketHandler
    {
        // The client shows three pages of ten rows per request and drops anything past that.
        private const int PageSize = 30;

        public override async Task ExecuteAsync(CBListPacket packet, ClientSession clientSession)
        {
            if (!npcDialogService.IsOpen(clientSession.Character, NpcDialogWindow.Bazaar))
            {
                return;
            }

            var definitivelist = await bazaarHttpClient.SearchBazaarAsync(new BazaarSearch
            {
                TypeFilter = packet.TypeFilter,
                SubTypeFilter = packet.SubTypeFilter,
                LevelFilter = packet.LevelFilter,
                RareFilter = packet.RareFilter,
                UpgradeFilter = packet.UpgradeFilter,
                OrderFilter = packet.OrderFilter,
                ItemVNumFilter = packet.ItemVNumFilter?.ToList() ?? new List<short>(),
                Language = clientSession.Account.Language,
                Index = packet.Index,
                PageSize = PageSize
            });
            var now = clock.GetCurrentInstant();

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
