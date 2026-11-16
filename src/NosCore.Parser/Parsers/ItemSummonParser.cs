//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Core.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class ItemSummonParser(IDao<ItemDto, short> itemDao, IDao<NpcMonsterDto, short> npcMonsterDao,
        IDao<ItemSummonDto, int> itemSummonDao, ILogger<ItemSummonParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertItemSummonsAsync()
        {
            var items = itemDao.LoadAll().Select(i => i.VNum).ToHashSet();
            var monsters = npcMonsterDao.LoadAll().Select(m => m.NpcMonsterVNum).ToHashSet();
            var id = 1;
            var rows = ItemSummons.All
                .Where(s => items.Contains(s.Key))
                .SelectMany(s => s.Value.Where(monsters.Contains).Select(monster => new ItemSummonDto
                {
                    ItemSummonId = id++,
                    ItemVNum = s.Key,
                    NpcMonsterVNum = monster,
                }))
                .ToList();

            await itemSummonDao.TryInsertOrUpdateAsync(rows).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.ITEM_SUMMONS_PARSED], rows.Count, rows.Select(r => r.ItemVNum).Distinct().Count());
        }
    }
}
