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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using Serilog;

namespace NosCore.Parser.Parsers
{
    public class ShopParser
    {
        private readonly ILogger _logger;
        private readonly IDao<MapNpcDto, int> _mapNpcDao;
        private readonly IDao<ShopDto, int> _shopDao;

        public ShopParser(IDao<ShopDto, int> shopDao, IDao<MapNpcDto, int> mapNpcDao, ILogger logger)
        {
            _shopDao = shopDao;
            _mapNpcDao = mapNpcDao;
            _logger = logger;
        }

        public async Task InsertShopsAsync(List<string[]> packetList)
        {
            var shopCounter = 0;
            var shops = new List<ShopDto>();
            var mapnpcdb = _mapNpcDao.LoadAll().ToList();
            var shopdb = _shopDao.LoadAll().ToList();
            foreach (var currentPacket in packetList.Where(o =>
                (o.Length > 6) && o[0].Equals("shop") && o[1].Equals("2"))
            )
            {
                var npcid = short.Parse(currentPacket[2]);
                var npc = mapnpcdb.FirstOrDefault(s => s.MapNpcId == npcid);
                if (npc == null)
                {
                    continue;
                }

                var name = new StringBuilder();
                for (var j = 6; j < currentPacket.Length; j++)
                {
                    name.Append($"{currentPacket[j]}");
                    if (j != currentPacket.Length - 1)
                    {
                        name.Append(" ");
                    }
                }

                var shop = new ShopDto
                {
                    MapNpcId = npc.MapNpcId,
                    MenuType = byte.Parse(currentPacket[4]),
                    ShopType = byte.Parse(currentPacket[5])
                };

                if ((shopdb.FirstOrDefault(s => s.MapNpcId == npc.MapNpcId) != null) ||
                    shops.Any(s => s.MapNpcId == npc.MapNpcId))
                {
                    continue;
                }

                shops.Add(shop);
                shopCounter++;
            }

            await _shopDao.TryInsertOrUpdateAsync(shops).ConfigureAwait(false);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SHOPS_PARSED),
                shopCounter);
        }
    }
}