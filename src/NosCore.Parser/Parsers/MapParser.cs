//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class MapParser(IDao<MapDto, short> mapDao, ILoggerFactory loggerFactory, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        // startMapId endMapId worldMap worldMapIndex name, then a DATA line holding the world region.
        // One line names every map from startMapId to endMapId.

        private readonly ILogger<MapParser> _logger = loggerFactory.CreateLogger<MapParser>();
        private readonly string _fileMapIdDat = $"{Path.DirectorySeparatorChar}MapIDData.dat";
        private readonly string _folderMap = $"{Path.DirectorySeparatorChar}map";

        public async Task<List<MapDto>> ParseDatAsync(string folder)
        {
            using var stream = new StreamReader(folder + _fileMapIdDat, Encoding.Default);
            var content = await stream.ReadToEndAsync();
            var maps = new Dictionary<short, MapDto>();
            foreach (var line in content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length < 5 || !short.TryParse(columns[0], out var start) || !short.TryParse(columns[1], out var end))
                {
                    continue;
                }

                for (int mapId = start; mapId <= end; mapId++)
                {
                    maps.TryAdd((short)mapId, new MapDto { MapId = (short)mapId, NameI18NKey = columns[4] });
                }
            }

            return maps.Values.ToList();
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
            _logger.LogInformation(logLanguage[LogLanguageKey.MAPS_PARSED], maps.Count);
        }
    }
}
