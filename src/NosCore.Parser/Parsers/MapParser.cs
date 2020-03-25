//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using Serilog;

namespace NosCore.Parser.Parsers
{
    public class MapParser
    {
        //{ID} {ID} {MapPoint} {MapPoint} {Name}
        //DATA 0

        private readonly string _fileMapIdDat = $"{Path.DirectorySeparatorChar}MapIDData.dat";
        private readonly string _folderMap = $"{Path.DirectorySeparatorChar}map";
        private readonly ILogger _logger;
        private readonly IGenericDao<MapDto> _mapDao;


        public MapParser(IGenericDao<MapDto> mapDao, ILogger logger)
        {
            _logger = logger;
            _mapDao = mapDao;
        }

        public List<MapDto> ParseDat(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                {nameof(MapDto.MapId), chunk => Convert.ToInt16(chunk.First(s=>char.IsDigit(s.Key.FirstOrDefault())).Value[0][0])},
                {nameof(MapDto.NameI18NKey), chunk => chunk.First(s=>char.IsDigit(s.Key.FirstOrDefault())).Value[0][4]}
            };
            var genericParser = new GenericParser<MapDto>(folder + _fileMapIdDat, "DATA 0", 0, actionList, _logger);
            return genericParser.GetDtos(" ");
        }

        public void InsertOrUpdateMaps(string folder, List<string[]> packetList)
        {
            var dictionaryId = ParseDat(folder);
            var folderMap = folder + _folderMap;
            var dictionaryMusic = packetList.Where(o => o[0].Equals("at") && (o.Length > 7))
                .GroupBy(x => x[2])
                .ToDictionary(x => x.Key, x => x.First()[7]);
            var maps = new DirectoryInfo(folderMap).GetFiles().Select(file=> new MapDto
            {
                NameI18NKey = dictionaryId.FirstOrDefault(s => s.MapId == int.Parse(file.Name))?.NameI18NKey ?? string.Empty,
                Music = dictionaryMusic.ContainsKey(file.Name) ? int.Parse(dictionaryMusic[file.Name]) : 0,
                MapId = short.Parse(file.Name),
                Data = File.ReadAllBytes(file.FullName),
                ShopAllowed = short.Parse(file.Name) == 147
            }).ToList();
           
            _mapDao.InsertOrUpdate(maps);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MAPS_PARSED), maps.Count);
        }
    }
}