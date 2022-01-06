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

using NosCore.Core.I18N;
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
    public class ShopItemParser
    {
        private readonly ILogger _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;
        private readonly IDao<ShopDto, int> _shopDao;
        private readonly IDao<ShopItemDto, int> _shopItemDao;

        public ShopItemParser(IDao<ShopItemDto, int> shopItemDao, IDao<ShopDto, int> shopDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _shopItemDao = shopItemDao;
            _shopDao = shopDao;
            _logger = logger;
            _logLanguage = logLanguage;
        }

        public async Task InsertShopItemsAsync(List<string[]> packetList)
        {
            var shopitems = new List<ShopItemDto>();
            var itemCounter = 0;
            byte type = 0;
            var shopItemdb = _shopItemDao.LoadAll().ToList();
            var shopdb = _shopDao.LoadAll().ToList();
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

            await _shopItemDao.TryInsertOrUpdateAsync(shopListItemDtos).ConfigureAwait(false);
            _logger.Information(_logLanguage[LogLanguageKey.SHOPITEMS_PARSED],
                itemCounter);
        }
    }
}