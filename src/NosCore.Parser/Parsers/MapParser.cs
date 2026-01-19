//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class MapParser(IDao<MapDto, short> mapDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        //{ID} {ID} {MapPoint} {MapPoint} {Name}
        //DATA 0

        private readonly string _fileMapIdDat = $"{Path.DirectorySeparatorChar}MapIDData.dat";
        private readonly string _folderMap = $"{Path.DirectorySeparatorChar}map";

        public Task<List<MapDto>> ParseDatAsync(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                {nameof(MapDto.MapId), chunk => Convert.ToInt16(chunk.First(s=>char.IsDigit(s.Key.FirstOrDefault())).Value[0][0])},
                {nameof(MapDto.NameI18NKey), chunk => chunk.First(s=>char.IsDigit(s.Key.FirstOrDefault())).Value[0][4]}
            };
            var genericParser = new GenericParser<MapDto>(folder + _fileMapIdDat, "DATA 0", 0, actionList, logger, logLanguage);
            return genericParser.GetDtosAsync(" ");
        }

        public async Task InsertOrUpdateMapsAsync(string folder, List<string[]> packetList)
        {
            var dictionaryId = await ParseDatAsync(folder);
            var folderMap = folder + _folderMap;
            var dictionaryMusic = packetList.Where(o => o[0].Equals("at") && (o.Length > 7))
                .GroupBy(x => x[2])
                .ToDictionary(x => x.Key, x => x.First()[7]);
            var maps = new DirectoryInfo(folderMap).GetFiles().Select(file => new MapDto
            {
                NameI18NKey = dictionaryId.FirstOrDefault(s => s.MapId == int.Parse(file.Name))?.NameI18NKey ?? string.Empty,
                Music = dictionaryMusic.TryGetValue(file.Name, out var value) ? int.Parse(value) : 0,
                MapId = short.Parse(file.Name),
                Data = File.ReadAllBytes(file.FullName),
                ShopAllowed = short.Parse(file.Name) == 147
            }).ToList();

            await mapDao.TryInsertOrUpdateAsync(maps);
            logger.Information(logLanguage[LogLanguageKey.MAPS_PARSED], maps.Count);
        }
    }
}
