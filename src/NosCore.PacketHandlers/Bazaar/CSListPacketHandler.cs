//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Auction;
using NosCore.Packets.ServerPackets.Inventory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CSListPacketHandler(IBazaarHub bazaarHttpClient, IClock clock) : PacketHandler<CSListPacket>,
        IWorldPacketHandler
    {
        public override async Task ExecuteAsync(CSListPacket packet, ClientSession clientSession)
        {
            var list = new List<RcsListPacket.RcsListElementPacket?>();
            var bzlist = await bazaarHttpClient.GetBazaar(-1, packet.Index, 50, 0, 0, 0, 0, 0,
                clientSession.Character.CharacterId);

            foreach (var bz in bzlist)
            {
                var soldedAmount = bz.BazaarItem!.Amount - bz.ItemInstance!.Amount;
                int amount = bz.BazaarItem.Amount;
                var isNosbazar = bz.BazaarItem.MedalUsed;
                var price = bz.BazaarItem.Price;
                var minutesLeft = (long)(bz.BazaarItem.DateStart.Plus(Duration.FromHours(bz.BazaarItem.Duration)) - clock.GetCurrentInstant())
                    .TotalMinutes;
                var status = minutesLeft >= 0 ? soldedAmount < amount ? BazaarStatusType.OnSale
                    : BazaarStatusType.Solded : BazaarStatusType.DelayExpired;
                if (status == BazaarStatusType.DelayExpired)
                {
                    minutesLeft =
                        (long)(bz.BazaarItem.DateStart.Plus(Duration.FromHours(bz.BazaarItem.Duration)).Plus(Duration.FromDays(isNosbazar ? 30 : 7)) -
                            clock.GetCurrentInstant()).TotalMinutes;
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
            });
        }
    }
}
