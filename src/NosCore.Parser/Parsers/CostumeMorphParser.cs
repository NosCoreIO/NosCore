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
    public class CostumeMorphParser(IDao<ItemDto, short> itemDao, IDao<NpcMonsterDto, short> npcMonsterDao,
        IDao<CostumeMorphDto, int> costumeMorphDao, ILogger<CostumeMorphParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        // The client draws a player morph of 1000 + a monster vnum as that monster.
        private const short MonsterMorphOffset = 1000;

        public async Task InsertCostumeMorphsAsync()
        {
            var items = itemDao.LoadAll().Select(i => i.VNum).ToHashSet();
            var monsters = npcMonsterDao.LoadAll().Select(m => m.NpcMonsterVNum).ToHashSet();
            var id = 1;
            var rows = CostumeScrolls.All
                .Where(s => items.Contains(s.Key))
                .SelectMany(s => s.Value.Where(monsters.Contains).Select(monster => new CostumeMorphDto
                {
                    CostumeMorphId = id++,
                    ItemVNum = s.Key,
                    Morph = (short)(MonsterMorphOffset + monster),
                }))
                .ToList();

            await costumeMorphDao.TryInsertOrUpdateAsync(rows).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.COSTUME_MORPHS_PARSED], rows.Count, rows.Select(r => r.ItemVNum).Distinct().Count());
        }
    }
}
