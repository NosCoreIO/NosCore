//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class ShopItemParser(IDao<ShopItemDto, int> shopItemDao, IDao<ShopDto, int> shopDao, ILogger<ShopItemParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertShopItemsAsync(IReadOnlyList<IPacket> packets)
        {
            var shopitems = new List<ShopItemDto>();
            var itemCounter = 0;
            byte type = 0;
            var shopItemdb = shopItemDao.LoadAll().ToList();
            var shopdb = shopDao.LoadAll().ToList();
            foreach (var packet in packets)
            {
                if (packet is ShoppingPacket shopping)
                {
                    type = (byte)shopping.ShopType;
                    continue;
                }

                if (packet is not NInvPacket { Items: { } items } nInv)
                {
                    continue;
                }

                var shop = shopdb.FirstOrDefault(s => s.MapNpcId == nInv.VisualId);
                if (shop == null)
                {
                    continue;
                }

                foreach (var item in items.OfType<NInvItemSubPacket>())
                {
                    var sitem = new ShopItemDto
                    {
                        ShopId = shop.ShopId,
                        Type = type,
                        Slot = (byte)item.Slot,
                        ItemVNum = item.VNum,
                        Rare = item.UpgradeDesign != null ? (short)(item.RareAmount ?? -1) : (short)0,
                        Upgrade = item.UpgradeDesign is { } upgrade ? (byte)upgrade : (byte)0
                    };

                    if (shopitems.Any(s =>
                            s.ItemVNum.Equals(sitem.ItemVNum) && s.ShopId.Equals(sitem.ShopId))
                        || shopItemdb.Where(s => s.ShopId == sitem.ShopId)
                            .Any(s => s.ItemVNum.Equals(sitem.ItemVNum)))
                    {
                        continue;
                    }

                    shopitems.Add(sitem);
                    itemCounter++;
                }
            }

            var groups = shopitems.GroupBy(s => s.ShopId);
            var shopListItemDtos = new List<ShopItemDto>();
            foreach (var group in groups)
            {
                var shopItemDtos = group.OrderBy(s => s.Slot).ToList();
                for (byte i = 0; i < shopItemDtos.Count; i++)
                {
                    shopItemDtos.ElementAt(i).Slot = i;
                }

                shopListItemDtos.AddRange(shopItemDtos);
            }

            await shopItemDao.TryInsertOrUpdateAsync(shopListItemDtos);
            logger.LogInformation(logLanguage[LogLanguageKey.SHOPITEMS_PARSED],
                itemCounter);
        }
    }
}
