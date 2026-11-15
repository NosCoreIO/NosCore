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
            byte type = 0;
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

                    if (shopitems.Any(s => s.ItemVNum.Equals(sitem.ItemVNum) && s.ShopId.Equals(sitem.ShopId)))
                    {
                        continue;
                    }

                    shopitems.Add(sitem);
                }
            }

            foreach (var group in shopitems.GroupBy(s => s.ShopId))
            {
                var shopItemDtos = group.OrderBy(s => s.Slot).ToList();
                for (byte i = 0; i < shopItemDtos.Count; i++)
                {
                    shopItemDtos[i].Slot = i;
                }
            }

            // The capture is the whole stock of the shops of the NPCs it shows.
            var capturedNpcs = CaptureSessions.NpcIds(packets);
            var capturedShops = shopdb.Where(s => capturedNpcs.Contains(s.MapNpcId)).Select(s => s.ShopId).ToHashSet();
            var stored = shopItemDao.LoadAll().ToList();
            var desired = shopitems.Select(s => (s.ShopId, s.ItemVNum)).ToHashSet();
            await shopItemDao.TryDeleteAsync(stored
                .Where(s => capturedShops.Contains(s.ShopId) && !desired.Contains((s.ShopId, s.ItemVNum)))
                .Select(s => s.ShopItemId).ToList());

            var storedByKey = stored.GroupBy(s => (s.ShopId, s.ItemVNum)).ToDictionary(g => g.Key, g => g.First());
            var changed = new List<ShopItemDto>();
            foreach (var item in shopitems)
            {
                if (!storedByKey.TryGetValue((item.ShopId, item.ItemVNum), out var row))
                {
                    changed.Add(item);
                }
                else if ((row.Type, row.Slot, row.Rare, row.Upgrade) != (item.Type, item.Slot, item.Rare, item.Upgrade))
                {
                    row.Type = item.Type;
                    row.Slot = item.Slot;
                    row.Rare = item.Rare;
                    row.Upgrade = item.Upgrade;
                    changed.Add(row);
                }
            }

            await shopItemDao.TryInsertOrUpdateAsync(changed);
            logger.LogInformation(logLanguage[LogLanguageKey.SHOPITEMS_PARSED], changed.Count);
        }
    }
}
