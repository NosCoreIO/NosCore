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
    public class HairStyleDyeParser(IDao<ItemDto, short> itemDao, IDao<HairStyleDyeDto, int> hairStyleDyeDao,
        ILogger<HairStyleDyeParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertHairStyleDyesAsync()
        {
            var items = itemDao.LoadAll().Select(i => i.VNum).ToHashSet();
            var id = 1;
            var rows = HairStyleDyes.All
                .Where(d => items.Contains(d.Key))
                .SelectMany(d => d.Value.Select(pair => new HairStyleDyeDto
                {
                    HairStyleDyeId = id++,
                    ItemVNum = d.Key,
                    HairStyle = pair.HairStyle,
                    DyedHairStyle = pair.Dyed,
                }))
                .ToList();

            await hairStyleDyeDao.TryInsertOrUpdateAsync(rows).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.HAIR_STYLE_DYES_PARSED], rows.Count, rows.Select(r => r.ItemVNum).Distinct().Count());
        }
    }
}
