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
    public class ItemTargetMonsterParser(IDao<ItemDto, short> itemDao, IDao<NpcMonsterDto, short> npcMonsterDao,
        IDao<ItemTargetMonsterDto, int> itemTargetMonsterDao, ILogger<ItemTargetMonsterParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertItemTargetMonstersAsync()
        {
            var items = itemDao.LoadAll().Select(i => i.VNum).ToHashSet();
            var monsters = npcMonsterDao.LoadAll().Select(m => m.NpcMonsterVNum).ToHashSet();
            var id = 1;
            var rows = ItemTargetMonsters.All
                .Where(s => items.Contains(s.Key))
                .SelectMany(s => s.Value.Where(monsters.Contains).Select(monster => new ItemTargetMonsterDto
                {
                    ItemTargetMonsterId = id++,
                    ItemVNum = s.Key,
                    NpcMonsterVNum = monster,
                }))
                .ToList();

            await itemTargetMonsterDao.TryInsertOrUpdateAsync(rows).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.ITEM_TARGET_MONSTERS_PARSED], rows.Count, rows.Select(r => r.ItemVNum).Distinct().Count());
        }
    }
}
