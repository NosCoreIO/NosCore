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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NosCore.Data.StaticEntities;
using NosCore.Database.DAL;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Parser.Parsers
{
    public class MapParser
    {
        private readonly string _fileMapIdDat = "\\MapIDData.dat";
        private readonly string _folderMap = "\\map";
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IGenericDao<MapDto> _mapDao = new GenericDao<Database.Entities.Map, MapDto>();
        public void InsertOrUpdateMaps(string folder, List<string[]> packetList)
        {
            var fileMapIdDat = folder + _fileMapIdDat;
            var folderMap = folder + _folderMap;
            var maps = new List<MapDto>();
            var dictionaryId = new Dictionary<int, string>();
            var dictionaryMusic = new Dictionary<int, int>();

            var i = 0;
            using (var mapIdStream = new StreamReader(fileMapIdDat, Encoding.Default))
            {
                string line;
                while ((line = mapIdStream.ReadLine()) != null)
                {
                    var linesave = line.Split(' ');
                    if (linesave.Length <= 1)
                    {
                        continue;
                    }

                    if (!int.TryParse(linesave[0], out var mapid))
                    {
                        continue;
                    }

                    if (!dictionaryId.ContainsKey(mapid))
                    {
                        dictionaryId.Add(mapid, linesave[4]);
                    }
                }

                mapIdStream.Close();
            }

            foreach (var linesave in packetList.Where(o => o[0].Equals("at")))
            {
                if (linesave.Length <= 7 || linesave[0] != "at")
                {
                    continue;
                }

                if (dictionaryMusic.ContainsKey(int.Parse(linesave[2])))
                {
                    continue;
                }

                dictionaryMusic.Add(int.Parse(linesave[2]), int.Parse(linesave[7]));
            }

            foreach (var file in new DirectoryInfo(folderMap).GetFiles())
            {
                var name = string.Empty;
                var music = 0;

                if (dictionaryId.ContainsKey(int.Parse(file.Name)))
                {
                    name = dictionaryId[int.Parse(file.Name)];
                }

                if (dictionaryMusic.ContainsKey(int.Parse(file.Name)))
                {
                    music = dictionaryMusic[int.Parse(file.Name)];
                }

                var map = new MapDto
                {
                    Name = name,
                    Music = music,
                    MapId = short.Parse(file.Name),
                    Data = File.ReadAllBytes(file.FullName),
                    ShopAllowed = short.Parse(file.Name) == 147
                };
                if (_mapDao.FirstOrDefault(s => s.MapId.Equals(map.MapId)) != null)
                {
                    continue; // Map already exists in list
                }

                maps.Add(map);
                i++;
            }

            IEnumerable<MapDto> mapDtos = maps;
            _mapDao.InsertOrUpdate(mapDtos);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MAPS_PARSED), i);
        }
    }
}