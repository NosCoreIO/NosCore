using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers
{
    public class MapParser
    {
        private readonly string _fileMapIdDat = "\\MapIDData.dat";
        private readonly string _folderMap = "\\map";

        public void InsertOrUpdateMaps(string folder, List<string[]> packetList)
        {
            var fileMapIdDat = folder + _fileMapIdDat;
            var folderMap = folder + _folderMap;
            var maps = new List<MapDTO>();
            var dictionaryId = new Dictionary<int, string>();
            var dictionaryMusic = new Dictionary<int, int>();

            var i = 0;
            using (var mapIdStream = new StreamReader(fileMapIdDat, Encoding.UTF8))
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

                var map = new MapDTO
                {
                    Name = name,
                    Music = music,
                    MapId = short.Parse(file.Name),
                    Data = File.ReadAllBytes(file.FullName),
                    ShopAllowed = short.Parse(file.Name) == 147
                };
                if (DAOFactory.MapDAO.FirstOrDefault(s => s.MapId.Equals(map.MapId)) != null)
                {
                    continue; // Map already exists in list
                }

                maps.Add(map);
                i++;
            }

            IEnumerable<MapDTO> mapDtos = maps;
            DAOFactory.MapDAO.InsertOrUpdate(mapDtos);
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MAPS_PARSED), i));
        }
    }
}