//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class MountMapSpeedParser(IDao<ItemDto, short> itemDao, IDao<MountMapSpeedDto, int> mountMapSpeedDao,
        ILogger<MountMapSpeedParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertMountMapSpeedsAsync()
        {
            var items = itemDao.LoadAll().Select(i => i.VNum).ToHashSet();
            var mounts = MountMapSpeeds.All.Where(m => items.Contains(m.Key)).ToList();
            var id = 1;
            var rows = mounts
                .SelectMany(m => m.Value
                    .SelectMany(region => region.Maps.Select(map => (Mount: m.Key, Map: map, region.Speed))))
                .GroupBy(r => (r.Mount, r.Map))
                .Select(g => new MountMapSpeedDto
                {
                    MountMapSpeedId = id++,
                    ItemVNum = g.Key.Mount,
                    MapId = g.Key.Map,
                    Speed = g.Max(r => r.Speed),
                })
                .ToList();

            await mountMapSpeedDao.TryInsertOrUpdateAsync(rows);
            logger.LogInformation(logLanguage[LogLanguageKey.MOUNT_MAP_SPEEDS_PARSED], rows.Count, mounts.Count);
        }
    }
}
