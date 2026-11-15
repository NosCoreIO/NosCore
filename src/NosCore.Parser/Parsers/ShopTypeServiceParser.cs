//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Shops;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    // shoptype.dat: "#" lines are comments (the service list and each NPC's name), every other
    // line is a shop type followed by up to six service ids. Only the services the server handles
    // are imported.
    public class ShopTypeServiceParser(IDao<ShopTypeServiceDto, int> shopTypeServiceDao,
        ILogger<ShopTypeServiceParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private readonly string _fileShopTypeDat = $"{Path.DirectorySeparatorChar}shoptype.dat";

        public async Task InsertShopTypeServicesAsync(string folder)
        {
            var rows = new List<ShopTypeServiceDto>();
            var id = 1;
            foreach (var line in await File.ReadAllLinesAsync(folder + _fileShopTypeDat))
            {
                var values = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < 2 || line.TrimStart().StartsWith('#') || !values.All(v => byte.TryParse(v, out _)))
                {
                    continue;
                }

                var shopType = byte.Parse(values[0]);
                foreach (var service in values.Skip(1).Select(v => (NpcService)byte.Parse(v)).Distinct()
                    .Where(s => Enum.IsDefined(s)))
                {
                    rows.Add(new ShopTypeServiceDto
                    {
                        ShopTypeServiceId = id++,
                        ShopType = shopType,
                        Service = service,
                    });
                }
            }

            await shopTypeServiceDao.TryInsertOrUpdateAsync(rows);
            logger.LogInformation(logLanguage[LogLanguageKey.SHOP_TYPE_SERVICES_PARSED], rows.Count,
                rows.Select(r => r.ShopType).Distinct().Count());
        }
    }
}
