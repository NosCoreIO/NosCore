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
    public class ShopParser(IDao<ShopDto, int> shopDao, IDao<ShopItemDto, int> shopItemDao, IDao<ShopSkillDto, int> shopSkillDao,
        IDao<MapNpcDto, int> mapNpcDao, ILogger<ShopParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertShopsAsync(IReadOnlyList<IPacket> packets)
        {
            var npcIds = mapNpcDao.LoadAll().Select(s => s.MapNpcId).ToHashSet();
            var captured = new Dictionary<int, ShopDto>();
            foreach (var packet in packets.OfType<ShopPacket>().Where(s => s.VisualType == VisualType.Npc))
            {
                if (npcIds.Contains((int)packet.VisualId) && packet.ShopType is { } shopType)
                {
                    captured.TryAdd((int)packet.VisualId, new ShopDto
                    {
                        MapNpcId = (int)packet.VisualId,
                        MenuType = packet.MenuType,
                        ShopType = shopType
                    });
                }
            }

            // A shop stored on a captured NPC that the capture does not show was put there by an
            // earlier import that mixed up NPC ids.
            var capturedNpcs = CaptureSessions.NpcIds(packets);
            var stored = shopDao.LoadAll().ToList();
            await CapturedRows.DeleteShopsAsync(shopDao, shopItemDao, shopSkillDao, stored
                .Where(s => capturedNpcs.Contains(s.MapNpcId) && !captured.ContainsKey(s.MapNpcId))
                .Select(s => s.ShopId).ToList());

            var shops = new List<ShopDto>();
            foreach (var shop in captured.Values)
            {
                var row = stored.FirstOrDefault(s => s.MapNpcId == shop.MapNpcId);
                if (row == null)
                {
                    shops.Add(shop);
                }
                else if ((row.MenuType, row.ShopType) != (shop.MenuType, shop.ShopType))
                {
                    row.MenuType = shop.MenuType;
                    row.ShopType = shop.ShopType;
                    shops.Add(row);
                }
            }

            await shopDao.TryInsertOrUpdateAsync(shops);
            logger.LogInformation(logLanguage[LogLanguageKey.SHOPS_PARSED], shops.Count);
        }
    }
}
