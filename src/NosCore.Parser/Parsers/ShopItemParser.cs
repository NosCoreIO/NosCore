//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class ShopItemParser(IDao<ShopItemDto, int> shopItemDao, IDao<ShopDto, int> shopDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertShopItemsAsync(List<string[]> packetList)
        {
            var shopitems = new List<ShopItemDto>();
            var itemCounter = 0;
            byte type = 0;
            var shopItemdb = shopItemDao.LoadAll().ToList();
            var shopdb = shopDao.LoadAll().ToList();
            foreach (var currentPacket in packetList.Where(o => o[0].Equals("n_inv") || o[0].Equals("shopping")))
            {
                if (currentPacket[0].Equals("n_inv"))
                {
                    var npcid = short.Parse(currentPacket[2]);
                    if (shopdb.FirstOrDefault(s => s.MapNpcId == npcid) == null)
                    {
                        continue;
                    }

                    for (var i = 5; i < currentPacket.Length; i++)
                    {
                        var item = currentPacket[i].Split('.');

                        if (item.Length < 5)
                        {
                            continue;
                        }

                        var sitem = new ShopItemDto
                        {
                            ShopId = shopdb.FirstOrDefault(s => s.MapNpcId == npcid)!
                                .ShopId,
                            Type = type,
                            Slot = byte.Parse(item[1]),
                            ItemVNum = short.Parse(item[2]),
                            Rare = item.Length == 6 ? sbyte.Parse(item[3]) : (short)0,
                            Upgrade = item.Length == 6 ? byte.Parse(item[4]) : (byte)0
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
                else if (currentPacket.Length > 3)
                {
                    type = byte.Parse(currentPacket[1]);
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
            logger.Information(logLanguage[LogLanguageKey.SHOPITEMS_PARSED],
                itemCounter);
        }
    }
}
