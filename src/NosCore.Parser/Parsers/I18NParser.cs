//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Text;
using NosCore.Data.I18N;
using NosCore.DAL;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Parser.Parsers
{
    public class I18NParser
    {
        private const string ActDescTxt = "\\_code_{0}_act_desc.txt";
        private const string BCardTxt = "\\_code_{0}_BCard.txt";
        private const string CardTxt = "\\_code_{0}_Card.txt";
        private const string ItemTxt = "\\_code_{0}_Item.txt";
        private const string MapIdDataTxt = "\\_code_{0}_MapIDData.txt";
        private const string MapPointDataTxt = "\\_code_{0}_MapPointData.txt";
        private const string MonsterTxt = "\\_code_{0}_monster.txt";
        private const string NpcTalkTxt = "\\_code_{0}_npctalk.txt";
        private const string QuestTxt = "\\_code_{0}_quest.txt";
        private const string SkillTxt = "\\_code_{0}_Skill.txt";
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private string _line;
        private string _folder;

        private string I18NTextFileName(string textfilename, RegionType region)
        {
            var regioncode = region.ToString().ToLower();
            regioncode = regioncode == "en" ? "uk" : regioncode;
            return string.Format(_folder + textfilename, regioncode);
        }

        public void InsertI18N(string folder)
        {
            _folder = folder;
            var actdesclist = DaoFactory.I18NActDescDao.LoadAll().ToList();
            foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
            {
                var actdescdtos = new List<I18NActDescDto>();
                try
                {
                    using (var stream = new StreamReader(I18NTextFileName(ActDescTxt, region),
                        Encoding.Default))
                    {
                        while ((_line = stream.ReadLine()) != null)
                        {
                            var currentLine = _line.Split('\t');
                            if (actdesclist.Find(s => s.Key == currentLine[0] && s.RegionType == region)
                                == null && currentLine.Length > 1 && !actdescdtos.Exists(s => s.Key == currentLine[0]))
                            {
                                actdescdtos.Add(new I18NActDescDto
                                {
                                    Key = currentLine[0],
                                    RegionType = region,
                                    Text = currentLine[1]
                                });
                            }
                        }

                        DaoFactory.I18NActDescDao.InsertOrUpdate(actdescdtos);

                        _logger.Information(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_ACTDESC_PARSED), actdescdtos.Count,
                            region));
                    }
                }
                catch (FileNotFoundException)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LANGUAGE_MISSING));
                }
            }

            var cardlist = DaoFactory.I18NCardDao.LoadAll().ToList();
            foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
            {
                var carddtos = new List<I18NCardDto>();
                try
                {
                    using (var stream = new StreamReader(I18NTextFileName(CardTxt, region),
                        Encoding.Default))
                    {
                        while ((_line = stream.ReadLine()) != null)
                        {
                            var currentLine = _line.Split('\t');
                            if (cardlist.Find(s => s.Key == currentLine[0] && s.RegionType == region) == null && currentLine.Length > 1 && !carddtos.Exists(s => s.Key == currentLine[0]))
                            {
                                carddtos.Add(new I18NCardDto
                                {
                                    Key = currentLine[0],
                                    RegionType = region,
                                    Text = currentLine[1]
                                });
                            }
                        }

                        DaoFactory.I18NCardDao.InsertOrUpdate(carddtos);

                        _logger.Information(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_CARD_PARSED), carddtos.Count,
                            region));
                    }
                }
                catch (FileNotFoundException)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LANGUAGE_MISSING));
                }
            }

            var bcardlist = DaoFactory.I18NbCardDao.LoadAll().ToList();
            foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
            {
                var bcarddtos = new List<I18NBCardDto>();
                try
                {
                    using (var stream = new StreamReader(I18NTextFileName(BCardTxt, region),
                        Encoding.Default))
                    {
                        while ((_line = stream.ReadLine()) != null)
                        {
                            var currentLine = _line.Split('\t');
                            if (bcardlist.Find(s => s.Key == currentLine[0] && s.RegionType == region)
                                == null && currentLine.Length > 1 && !bcarddtos.Exists(s => s.Key == currentLine[0]))
                            {
                                bcarddtos.Add(new I18NBCardDto
                                {
                                    Key = currentLine[0],
                                    RegionType = region,
                                    Text = currentLine[1]
                                });
                            }
                        }

                        DaoFactory.I18NbCardDao.InsertOrUpdate(bcarddtos);

                        _logger.Information(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_BCARD_PARSED), bcarddtos.Count,
                            region));
                    }
                }
                catch (FileNotFoundException)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LANGUAGE_MISSING));
                }
            }

            var itemlist = DaoFactory.I18NItemDao.LoadAll().ToList();
            foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
            {
                var itemdtos = new List<I18NItemDto>();
                try
                {
                    using (var stream = new StreamReader(I18NTextFileName(ItemTxt, region),
                        Encoding.Default))
                    {
                        while ((_line = stream.ReadLine()) != null)
                        {
                            var currentLine = _line.Split('\t');
                            if (itemlist.Find(s => s.Key == currentLine[0] && s.RegionType == region) == null && currentLine.Length > 1 && !itemdtos.Exists(s => s.Key == currentLine[0]))
                            {
                                itemdtos.Add(new I18NItemDto
                                {
                                    Key = currentLine[0],
                                    RegionType = region,
                                    Text = currentLine[1]
                                });
                            }
                        }

                        DaoFactory.I18NItemDao.InsertOrUpdate(itemdtos);

                        _logger.Information(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_ITEM_PARSED), itemdtos.Count,
                            region));
                    }
                }
                catch (FileNotFoundException)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LANGUAGE_MISSING));
                }
            }

            var mapiddatalist = DaoFactory.I18NMapIdDataDao.LoadAll().ToList();
            foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
            {
                var mapiddatadtos = new List<I18NMapIdDataDto>();
                try
                {
                    using (var stream = new StreamReader(I18NTextFileName(MapIdDataTxt, region),
                        Encoding.Default))
                    {
                        while ((_line = stream.ReadLine()) != null)
                        {
                            var currentLine = _line.Split('\t');
                            if (mapiddatalist.Find(s => s.Key == currentLine[0] && s.RegionType == region)
                                == null && currentLine.Length > 1 && !mapiddatadtos.Exists(s => s.Key == currentLine[0]))
                            {
                                mapiddatadtos.Add(new I18NMapIdDataDto
                                {
                                    Key = currentLine[0],
                                    RegionType = region,
                                    Text = currentLine[1]
                                });
                            }
                        }

                        DaoFactory.I18NMapIdDataDao.InsertOrUpdate(mapiddatadtos);

                        _logger.Information(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_MAPIDDATA_PARSED),
                            mapiddatadtos.Count, region));
                    }
                }
                catch (FileNotFoundException)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LANGUAGE_MISSING));
                }
            }

            var mappointdatalist = DaoFactory.I18NMapPointDataDao.LoadAll().ToList();
            foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
            {
                var mappointdatadtos = new List<I18NMapPointDataDto>();
                try
                {
                    using (var stream = new StreamReader(I18NTextFileName(MapPointDataTxt, region),
                        Encoding.Default))
                    {
                        while ((_line = stream.ReadLine()) != null)
                        {
                            var currentLine = _line.Split('\t');
                            if (mappointdatalist.Find(s =>
                                s.Key == currentLine[0] && s.RegionType == region) == null && currentLine.Length > 1 && !mappointdatadtos.Exists(s => s.Key == currentLine[0]))
                            {
                                mappointdatadtos.Add(new I18NMapPointDataDto
                                {
                                    Key = currentLine[0],
                                    RegionType = region,
                                    Text = currentLine[1]
                                });
                            }
                        }

                        DaoFactory.I18NMapPointDataDao.InsertOrUpdate(mappointdatadtos);

                        _logger.Information(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_MAPPOINTDATA_PARSED),
                            mappointdatadtos.Count, region));
                    }
                }
                catch (FileNotFoundException)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LANGUAGE_MISSING));
                }
            }

            var npcmonsterlist = DaoFactory.I18NNpcMonsterDao.LoadAll().ToList();
            foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
            {
                var npcmonsterdto = new List<I18NNpcMonsterDto>();
                try
                {
                    using (var stream = new StreamReader(I18NTextFileName(MonsterTxt, region),
                        Encoding.Default))
                    {
                        while ((_line = stream.ReadLine()) != null)
                        {
                            var currentLine = _line.Split('\t');
                            if (npcmonsterlist.Find(s => s.Key == currentLine[0] && s.RegionType == region)
                                == null && currentLine.Length > 1 && !npcmonsterdto.Exists(s => s.Key == currentLine[0]))
                            {
                                npcmonsterdto.Add(new I18NNpcMonsterDto
                                {
                                    Key = currentLine[0],
                                    RegionType = region,
                                    Text = currentLine[1]
                                });
                            }
                        }

                        DaoFactory.I18NNpcMonsterDao.InsertOrUpdate(npcmonsterdto);

                        _logger.Information(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_MPCMONSTER_PARSED),
                            npcmonsterdto.Count, region));
                    }
                }
                catch (FileNotFoundException)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LANGUAGE_MISSING));
                }
            }

            var npcmonstertalklist = DaoFactory.I18NNpcMonsterTalkDao.LoadAll().ToList();
            foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
            {
                var npctalkdtos = new List<I18NNpcMonsterTalkDto>();
                try
                {
                    using (var stream = new StreamReader(I18NTextFileName(NpcTalkTxt, region),
                        Encoding.Default))
                    {
                        while ((_line = stream.ReadLine()) != null)
                        {
                            var currentLine = _line.Split('\t');
                            if (npcmonstertalklist.Find(
                                s => s.Key == currentLine[0] && s.RegionType == region) == null && currentLine.Length > 1 && !npctalkdtos.Exists(s => s.Key == currentLine[0]))
                            {
                                npctalkdtos.Add(new I18NNpcMonsterTalkDto
                                {
                                    Key = currentLine[0],
                                    RegionType = region,
                                    Text = currentLine[1]
                                });
                            }
                        }

                        DaoFactory.I18NNpcMonsterTalkDao.InsertOrUpdate(npctalkdtos);

                        _logger.Information(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_NPCMONSTERTALK_PARSED),
                            npctalkdtos.Count, region));
                    }
                }
                catch (FileNotFoundException)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LANGUAGE_MISSING));
                }
            }

            var questlist = DaoFactory.I18NQuestDao.LoadAll().ToList();
            foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
            {
                var questdtos = new List<I18NQuestDto>();
                try
                {
                    using (var stream = new StreamReader(I18NTextFileName(QuestTxt, region),
                        Encoding.Default))
                    {
                        while ((_line = stream.ReadLine()) != null)
                        {
                            var currentLine = _line.Split('\t');
                            if (questlist.Find(s => s.Key == currentLine[0] && s.RegionType == region)
                                == null && currentLine.Length > 1 && !questdtos.Exists(s => s.Key == currentLine[0]))
                            {
                                questdtos.Add(new I18NQuestDto
                                {
                                    Key = currentLine[0],
                                    RegionType = region,
                                    Text = currentLine[1]
                                });
                            }
                        }

                        DaoFactory.I18NQuestDao.InsertOrUpdate(questdtos);

                        _logger.Information(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_QUEST_PARSED), questdtos.Count,
                            region));
                    }
                }
                catch (FileNotFoundException)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LANGUAGE_MISSING));
                }
            }

            var skilllist = DaoFactory.I18NSkillDao.LoadAll().ToList();
            foreach (RegionType region in Enum.GetValues(typeof(RegionType)))
            {
                var skilldtos = new List<I18NSkillDto>();
                try
                {
                    using (var stream = new StreamReader(I18NTextFileName(SkillTxt, region),
                        Encoding.Default))
                    {
                        while ((_line = stream.ReadLine()) != null)
                        {
                            var currentLine = _line.Split('\t');
                            if (skilllist.Find(s => s.Key == currentLine[0] && s.RegionType == region)
                                == null && !skilldtos.Exists(s => s.Key == currentLine[0]))
                            {
                                skilldtos.Add(new I18NSkillDto
                                {
                                    Key = currentLine[0],
                                    RegionType = region,
                                    Text = currentLine[1]
                                });
                            }
                        }

                        DaoFactory.I18NSkillDao.InsertOrUpdate(skilldtos);

                        _logger.Information(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.I18N_SKILL_PARSED), skilldtos.Count,
                            region));
                    }
                }
                catch (FileNotFoundException)
                {
                   _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LANGUAGE_MISSING));
                }
            }
        }
    }
}