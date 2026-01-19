//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class ShopParser(IDao<ShopDto, int> shopDao, IDao<MapNpcDto, int> mapNpcDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertShopsAsync(List<string[]> packetList)
        {
            var shopCounter = 0;
            var shops = new List<ShopDto>();
            var mapnpcdb = mapNpcDao.LoadAll().ToList();
            var shopdb = shopDao.LoadAll().ToList();
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

            await shopDao.TryInsertOrUpdateAsync(shops);
            logger.Information(logLanguage[LogLanguageKey.SHOPS_PARSED],
                shopCounter);
        }
    }
}
