//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class ShopParser(IDao<ShopDto, int> shopDao, IDao<MapNpcDto, int> mapNpcDao, ILogger<ShopParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertShopsAsync(IReadOnlyList<IPacket> packets)
        {
            var shopCounter = 0;
            var shops = new List<ShopDto>();
            var mapnpcdb = mapNpcDao.LoadAll().ToList();
            var shopdb = shopDao.LoadAll().ToList();
            foreach (var currentPacket in packets.OfType<ShopPacket>().Where(s => s.VisualType == VisualType.Npc))
            {
                var npc = mapnpcdb.FirstOrDefault(s => s.MapNpcId == currentPacket.VisualId);
                if (npc == null || currentPacket.ShopType is not { } shopType)
                {
                    continue;
                }

                var shop = new ShopDto
                {
                    MapNpcId = npc.MapNpcId,
                    MenuType = currentPacket.MenuType,
                    ShopType = shopType
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
            logger.LogInformation(logLanguage[LogLanguageKey.SHOPS_PARSED],
                shopCounter);
        }
    }
}
