//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NosCore.Core.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers
{
    public class PartnerSpecialistNameParser(IDao<ItemDto, short> itemDao, ILogger<PartnerSpecialistNameParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        public async Task InsertPartnerSpecialistNamesAsync()
        {
            var named = new List<ItemDto>();
            foreach (var item in itemDao.LoadAll())
            {
                if (PartnerSpecialistNames.All.TryGetValue(item.VNum, out var name))
                {
                    item.PartnerSpecialistName = name;
                    named.Add(item);
                }
            }

            await itemDao.TryInsertOrUpdateAsync(named).OrThrowAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.PARTNER_SPECIALIST_NAMES_PARSED], named.Count);
        }
    }
}
