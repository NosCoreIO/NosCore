//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    //#{NPC name}
    //{ShopType} {Service} {Service}...
    public class ShopTypeServiceParser(IDao<ShopTypeServiceDto, short> shopTypeServiceDao,
        ILogger<ShopTypeServiceParser> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private const string FileName = "shoptype.dat";

        public async Task InsertShopTypeServicesAsync(string folder)
        {
            var rows = Parse(await File.ReadAllTextAsync(Path.Combine(folder, FileName)));
            await shopTypeServiceDao.TryInsertOrUpdateAsync(rows);
            logger.LogInformation(logLanguage[LogLanguageKey.SHOP_TYPE_SERVICES_PARSED], rows.Count,
                rows.Select(r => r.ShopType).Distinct().Count());
        }

        public static List<ShopTypeServiceDto> Parse(string content)
        {
            short id = 1;
            return content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith('#'))
                .Select(line => line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
                .Where(columns => columns.Length > 1 && byte.TryParse(columns[0], out _))
                .SelectMany(columns => columns.Skip(1)
                    .Select(service => (ShopType: byte.Parse(columns[0]), Service: byte.TryParse(service, out var s) ? s : (byte)0)))
                .Where(row => Enum.IsDefined((NpcService)row.Service))
                .Distinct()
                .Select(row => new ShopTypeServiceDto
                {
                    ShopTypeServiceId = id++,
                    ShopType = row.ShopType,
                    Service = (NpcService)row.Service,
                })
                .ToList();
        }
    }
}
