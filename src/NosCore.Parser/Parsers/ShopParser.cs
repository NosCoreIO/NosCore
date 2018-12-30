//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Parser.Parsers
{
    internal class ShopParser
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        public void InsertShops(List<string[]> packetList)
        {
            int shopCounter = 0;
            var shops = new List<ShopDto>();
            foreach (var currentPacket in packetList.Where(o => o.Length > 6 && o[0].Equals("shop") && o[1].Equals("2")))
            {
                short npcid = short.Parse(currentPacket[2]);
                var npc = DaoFactory.MapNpcDao.FirstOrDefault(s => s.MapNpcId == npcid);
                if (npc == null)
                {
                    continue;
                }
                string name = string.Empty;
                for (int j = 6; j < currentPacket.Length; j++)
                {
                    name += $"{currentPacket[j]} ";
                }
                name = name.Trim();

                var shop = new ShopDto
                {
                    Name = name,
                    MapNpcId = npc.MapNpcId,
                    MenuType = byte.Parse(currentPacket[4]),
                    ShopType = byte.Parse(currentPacket[5])
                };

                if (DaoFactory.ShopDao.FirstOrDefault(s => s.MapNpcId == npc.MapNpcId) != null || shops.Any(s => s.MapNpcId == npc.MapNpcId))
                {
                    continue;
                }
                shops.Add(shop);
                shopCounter++;
            }
            
            IEnumerable<ShopDto> shopDtos = shops;
            DaoFactory.ShopDao.InsertOrUpdate(shopDtos);

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SHOPS_PARSED),
                shopCounter);
        }
    }
}