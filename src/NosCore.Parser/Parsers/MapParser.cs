using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NosCore.Configuration;
using NosCore.Shared.Logger;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using System;

namespace NosCore.Parser.Parsers
{
    public class MapParser
    {
        private readonly string _fileMapIdDat = $"\\MapIDData.dat";
        private readonly string _folderMap = $"\\map";
        private readonly Dictionary<int, string> _dictionaryId = new Dictionary<int, string>();
        private readonly Dictionary<string, string> dictionaryIdLang = new Dictionary<string, string>();
        private readonly Dictionary<int, int> _dictionaryMusic = new Dictionary<int, int>();
        private readonly ParserConfiguration configuration;

        public MapParser(ParserConfiguration conf)
        {
            configuration = conf;
        }

        public void InsertOrUpdateMaps(string folder, List<string[]> packetList)
        {
            string _fileMapIdLang = $"\\_code_{(string.Equals(configuration.Language, "en", StringComparison.OrdinalIgnoreCase) ? "uk" : configuration.Language)}_MapIDData.txt";
            string fileMapIdDat = folder + _fileMapIdDat;
            string fileMapIdLang = folder + _fileMapIdLang;
            string folderMap = folder + _folderMap;
            List<MapDTO> maps = new List<MapDTO>();
            Dictionary<int, string> dictionaryId = new Dictionary<int, string>();
            Dictionary<int, int> dictionaryMusic = new Dictionary<int, int>();

            string line;
            int i = 0;
            using (StreamReader mapIdStream = new StreamReader(fileMapIdDat, Encoding.UTF8))
            {
                while ((line = mapIdStream.ReadLine()) != null)
                {
                    string[] linesave = line.Split(' ');
                    if (linesave.Length <= 1)
                    {
                        continue;
                    }
                    if (!int.TryParse(linesave[0], out int mapid))
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

            using (StreamReader mapIdLangStream = new StreamReader(fileMapIdLang, Encoding.UTF8))
            {
                while ((line = mapIdLangStream.ReadLine()) != null)
                {
                    string[] linesave = line.Split('\t');
                    if (linesave.Length <= 1 || dictionaryIdLang.ContainsKey(linesave[0]))
                    {
                        continue;
                    }
                    dictionaryIdLang.Add(linesave[0], linesave[1]);
                }
                mapIdLangStream.Close();
            }

            foreach (string[] linesave in packetList.Where(o => o[0].Equals("at")))
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

            foreach (FileInfo file in new DirectoryInfo(folderMap).GetFiles())
            {
                string name = string.Empty;
                int music = 0;

                if (dictionaryId.ContainsKey(int.Parse(file.Name)) && dictionaryIdLang.ContainsKey(dictionaryId[int.Parse(file.Name)]))
                {
                    name = dictionaryIdLang[dictionaryId[int.Parse(file.Name)]];
                }
                if (dictionaryMusic.ContainsKey(int.Parse(file.Name)))
                {
                    music = dictionaryMusic[int.Parse(file.Name)];
                }
                MapDTO map = new MapDTO
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
            Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MAPS_PARSED), i));
        }
    }
}
