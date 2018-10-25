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
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Parser.Parsers
{
    internal class NpcMonsterParser
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private const string FileNpcId = "\\monster.dat";
        private string _folder;

        internal void InsertNpcMonsters(string folder)
        {
            _folder = folder;
            var basicHp = new int[100];
            var basicPrimaryMp = new int[100];
            var basicSecondaryMp = new int[100];
            var basicXp = new int[100];
            var basicJXp = new int[100];

            // basicHPLoad
            var baseHp = 138;
            var hPbasup = 18;
            for (var i = 0; i < 100; i++)
            {
                basicHp[i] = baseHp;
                hPbasup++;
                baseHp += hPbasup;

                if (i == 37)
                {
                    baseHp = 1765;
                    hPbasup = 65;
                }

                if (i < 41)
                {
                    continue;
                }

                if ((99 - i) % 8 == 0)
                {
                    hPbasup++;
                }
            }

            //Race == 0
            basicPrimaryMp[0] = 10;
            basicPrimaryMp[1] = 10;
            basicPrimaryMp[2] = 15;

            var primaryBasup = 5;
            byte count = 0;
            var isStable = true;
            var isDouble = false;

            for (uint i = 3; i < 100; i++)
            {
                if (i % 10 == 1)
                {
                    basicPrimaryMp[i] += basicPrimaryMp[i - 1] + (primaryBasup * 2);
                    continue;
                }

                if (!isStable)
                {
                    primaryBasup++;
                    count++;

                    if (count == 2)
                    {
                        if (isDouble)
                        {
                            isDouble = false;
                        }
                        else
                        {
                            isStable = true;
                            isDouble = true;
                            count = 0;
                        }
                    }

                    if (count == 4)
                    {
                        isStable = true;
                        count = 0;
                    }
                }
                else
                {
                    count++;
                    if (count == 2)
                    {
                        isStable = false;
                        count = 0;
                    }
                }

                basicPrimaryMp[i] = basicPrimaryMp[i - (i % 10 == 2 ? 2 : 1)] + primaryBasup;
            }

            // Race == 2
            basicSecondaryMp[0] = 60;
            basicSecondaryMp[1] = 60;
            basicSecondaryMp[2] = 78;

            var secondaryBasup = 18;
            var boostup = false;

            for (uint i = 3; i < 100; i++)
            {
                if (i % 10 == 1)
                {
                    basicSecondaryMp[i] += basicSecondaryMp[i - 1] + (int)i + 10;
                    continue;
                }

                if (boostup)
                {
                    secondaryBasup += 3;
                    boostup = false;
                }
                else
                {
                    secondaryBasup++;
                    boostup = true;
                }

                basicSecondaryMp[i] = basicSecondaryMp[i - (i % 10 == 2 ? 2 : 1)] + secondaryBasup;
            }

            // basicXPLoad
            for (var i = 0; i < 100; i++)
            {
                basicXp[i] = i * 180;
            }

            // basicJXpLoad
            for (var i = 0; i < 100; i++)
            {
                basicJXp[i] = 360;
            }

            var npcs = new List<NpcMonsterDto>();

            // Store like this: (vnum, (name, level))
            var npc = new NpcMonsterDto();
            var drops = new List<DropDto>();
            var monstercards = new List<BCardDto>();
            var skills = new List<NpcMonsterSkillDto>();
            var itemAreaBegin = false;
            var counter = 0;
            long unknownData = 0;

            using (var npcIdStream = new StreamReader(_folder + FileNpcId, Encoding.Default))
            {
                string line;
                while ((line = npcIdStream.ReadLine()) != null)
                {
                    var currentLine = line.Split('\t');

                    if (currentLine.Length > 2 && currentLine[1] == "VNUM")
                    {
                        npc = new NpcMonsterDto
                        {
                            NpcMonsterVNum = Convert.ToInt16(currentLine[2])
                        };
                        itemAreaBegin = true;
                        unknownData = 0;
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "NAME")
                    {
                        npc.Name = currentLine[2];
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "LEVEL")
                    {
                        if (!itemAreaBegin)
                        {
                            continue;
                        }

                        npc.Level = Convert.ToByte(currentLine[2]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "RACE")
                    {
                        npc.Race = Convert.ToByte(currentLine[2]);
                        npc.RaceType = Convert.ToByte(currentLine[3]);
                    }
                    else if (currentLine.Length > 7 && currentLine[1] == "ATTRIB")
                    {
                        npc.Element = Convert.ToByte(currentLine[2]);
                        npc.ElementRate = Convert.ToInt16(currentLine[3]);
                        npc.FireResistance = Convert.ToSByte(currentLine[4]);
                        npc.WaterResistance = Convert.ToSByte(currentLine[5]);
                        npc.LightResistance = Convert.ToSByte(currentLine[6]);
                        npc.DarkResistance = Convert.ToSByte(currentLine[7]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "HP/MP")
                    {
                        npc.MaxHp = Convert.ToInt32(currentLine[2]) + basicHp[npc.Level];
                        npc.MaxMp = Convert.ToInt32(currentLine[3]) + npc.Race == 0 ? basicPrimaryMp[npc.Level]
                            : basicSecondaryMp[npc.Level];
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "EXP")
                    {
                        npc.Xp = Math.Abs(Convert.ToInt32(currentLine[2]) + basicXp[npc.Level]);
                        npc.JobXp = Convert.ToInt32(currentLine[3]) + basicJXp[npc.Level];

                        //TODO find HeroXP algorithm
                        switch (npc.NpcMonsterVNum)
                        {
                            case 2510:
                            case 2501:
                                npc.HeroXp = 881;
                                break;

                            case 2512:
                            case 2502:
                                npc.HeroXp = 884;
                                break;

                            case 2503:
                                npc.HeroXp = 1013;
                                break;

                            case 2505:
                                npc.HeroXp = 871;
                                break;

                            case 2506:
                                npc.HeroXp = 765;
                                break;

                            case 2507:
                                npc.HeroXp = 803;
                                break;

                            case 2508:
                                npc.HeroXp = 825;
                                break;

                            case 2500:
                            case 2509:
                            case 2511:
                                npc.HeroXp = 879;
                                break;

                            case 2513:
                                npc.HeroXp = 1075;
                                break;

                            case 2515:
                                npc.HeroXp = 3803;
                                break;

                            case 2516:
                                npc.HeroXp = 836;
                                break;

                            case 2517:
                                npc.HeroXp = 450;
                                break;

                            case 2518:
                                npc.HeroXp = 911;
                                break;

                            case 2519:
                                npc.HeroXp = 845;
                                break;

                            case 2520:
                                npc.HeroXp = 3682;
                                break;

                            case 2521:
                                npc.HeroXp = 401;
                                break;

                            case 2522:
                                npc.HeroXp = 471;
                                break;

                            case 2523:
                                npc.HeroXp = 328;
                                break;

                            case 2524:
                                npc.HeroXp = 12718;
                                break;

                            case 2525:
                                npc.HeroXp = 412;
                                break;

                            case 2526:
                                npc.HeroXp = 11157;
                                break;

                            case 2527:
                                npc.HeroXp = 18057;
                                break;

                            case 2530:
                                npc.HeroXp = 28756;
                                break;

                            case 2559:
                                npc.HeroXp = 1308;
                                break;

                            case 2560:
                                npc.HeroXp = 1234;
                                break;

                            case 2561:
                                npc.HeroXp = 1168;
                                break;

                            case 2562:
                                npc.HeroXp = 959;
                                break;

                            case 2563:
                                npc.HeroXp = 947;
                                break;

                            case 2564:
                                npc.HeroXp = 952;
                                break;

                            case 2566:
                                npc.HeroXp = 1097;
                                break;

                            case 2567:
                                npc.HeroXp = 1096;
                                break;

                            case 2568:
                                npc.HeroXp = 4340;
                                break;

                            case 2569:
                                npc.HeroXp = 3534;
                                break;

                            case 2570:
                                npc.HeroXp = 4343;
                                break;

                            case 2571:
                                npc.HeroXp = 2205;
                                break;

                            case 2572:
                                npc.HeroXp = 5632;
                                break;

                            case 2573:
                                npc.HeroXp = 3756;
                                break;

                            default:
                                npc.HeroXp = 0;
                                break;
                        }
                    }
                    else if (currentLine.Length > 6 && currentLine[1] == "PREATT")
                    {
                        npc.IsHostile = currentLine[2] != "0";
                        npc.NoticeRange = Convert.ToByte(currentLine[4]);
                        npc.Speed = Convert.ToByte(currentLine[5]);
                        npc.RespawnTime = Convert.ToInt32(currentLine[6]);
                    }
                    else if (currentLine.Length > 6 && currentLine[1] == "WEAPON")
                    {
                        switch (currentLine[3])
                        {
                            case "1":
                                npc.DamageMinimum = Convert.ToInt16(((Convert.ToInt16(currentLine[2]) - 1) * 4) + 32
                                    + Convert.ToInt16(currentLine[4])
                                    + Math.Round(Convert.ToDecimal((npc.Level - 1) / 5)));
                                npc.DamageMaximum = Convert.ToInt16(((Convert.ToInt16(currentLine[2]) - 1) * 6) + 40
                                    + Convert.ToInt16(currentLine[5])
                                    - Math.Round(Convert.ToDecimal((npc.Level - 1) / 5)));
                                npc.Concentrate = Convert.ToInt16(((Convert.ToInt16(currentLine[2]) - 1) * 5) + 27
                                    + Convert.ToInt16(currentLine[6]));
                                npc.CriticalChance = Convert.ToByte(4 + Convert.ToInt16(currentLine[7]));
                                npc.CriticalRate = Convert.ToInt16(70 + Convert.ToInt16(currentLine[8]));
                                break;
                            case "2":
                                npc.DamageMinimum = Convert.ToInt16((Convert.ToInt16(currentLine[2]) * 6.5f) + 23
                                    + Convert.ToInt16(currentLine[4]));
                                npc.DamageMaximum = Convert.ToInt16(((Convert.ToInt16(currentLine[2]) - 1) * 8) + 38
                                    + Convert.ToInt16(currentLine[5]));
                                npc.Concentrate = Convert.ToInt16(70 + Convert.ToInt16(currentLine[6]));
                                break;
                            default:
                                continue;
                        }
                    }
                    else if (currentLine.Length > 6 && currentLine[1] == "ARMOR")
                    {
                        npc.CloseDefence = Convert.ToInt16(((Convert.ToInt16(currentLine[2]) - 1) * 2) + 18);
                        npc.DistanceDefence = Convert.ToInt16(((Convert.ToInt16(currentLine[2]) - 1) * 3) + 17);
                        npc.MagicDefence = Convert.ToInt16(((Convert.ToInt16(currentLine[2]) - 1) * 2) + 13);
                        npc.DefenceDodge = Convert.ToInt16(((Convert.ToInt16(currentLine[2]) - 1) * 5) + 31);
                        npc.DistanceDefenceDodge = Convert.ToInt16(((Convert.ToInt16(currentLine[2]) - 1) * 5) + 31);
                    }
                    else if (currentLine.Length > 7 && currentLine[1] == "ETC")
                    {
                        if (npc.NpcMonsterVNum >= 588 && npc.NpcMonsterVNum <= 607)
                        {
                            npc.MonsterType = MonsterType.Elite;
                        }
                        unknownData = Convert.ToInt64(currentLine[2]);
                        switch (unknownData)
                        {
                            case -2147481593:
                                npc.MonsterType = MonsterType.Special;
                                break;
                            case -2147483616:
                            case -2147483647:
                            case -2147483646:
                                npc.NoAggresiveIcon = (npc.Race == 8 && npc.RaceType == 0);
                                break;
                            default:
                                continue;
                        }
                    }
                    else if (currentLine.Length > 6 && currentLine[1] == "SETTING")
                    {
                        if (currentLine[4] == "0")
                        {
                            continue;
                        }

                        npc.VNumRequired = Convert.ToInt16(currentLine[4]);
                        npc.AmountRequired = 1;
                    }
                    else if (currentLine.Length > 4 && currentLine[1] == "PETINFO")
                    {
                        if (npc.VNumRequired != 0 || (unknownData != -2147481593 && unknownData != -2147481599
                            && unknownData != -1610610681))
                        {
                            continue;
                        }

                        npc.VNumRequired = Convert.ToInt16(currentLine[2]);
                        npc.AmountRequired = Convert.ToByte(currentLine[3]);
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "EFF")
                    {
                        npc.BasicSkill = Convert.ToInt16(currentLine[2]);
                    }
                    else if (currentLine.Length > 8 && currentLine[1] == "ZSKILL")
                    {
                        npc.AttackClass = Convert.ToByte(currentLine[2]);
                        npc.BasicRange = Convert.ToByte(currentLine[3]);
                        npc.BasicArea = Convert.ToByte(currentLine[5]);
                        npc.BasicCooldown = Convert.ToInt16(currentLine[6]);
                    }
                    else if (currentLine.Length > 4 && currentLine[1] == "WINFO")
                    {
                        npc.AttackUpgrade = Convert.ToByte(unknownData == 1 ? currentLine[2] : currentLine[4]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "AINFO")
                    {
                        npc.DefenceUpgrade = Convert.ToByte(unknownData == 1 ? currentLine[2] : currentLine[3]);
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "SKILL")
                    {
                        for (var i = 2; i < currentLine.Length - 3; i += 3)
                        {
                            var vnum = short.Parse(currentLine[i]);
                            if (vnum == -1 || vnum == 0)
                            {
                                break;
                            }

                            if (DaoFactory.SkillDao.FirstOrDefault(s => s.SkillVNum.Equals(vnum)) == null
                                || DaoFactory.NpcMonsterSkillDao.Where(s => s.NpcMonsterVNum.Equals(npc.NpcMonsterVNum))
                                    .Count(s => s.SkillVNum == vnum) != 0)
                            {
                                continue;
                            }

                            skills.Add(new NpcMonsterSkillDto
                            {
                                SkillVNum = vnum,
                                Rate = Convert.ToInt16(currentLine[i + 1]),
                                NpcMonsterVNum = npc.NpcMonsterVNum
                            });
                        }
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "CARD")
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            var type = (byte) int.Parse(currentLine[(5 * i) + 2]);
                            if (type == 0 || type == 255)
                            {
                                continue;
                            }

                            var first = int.Parse(currentLine[(5 * i) + 3]);
                            var itemCard = new BCardDto
                            {
                                NpcMonsterVNum = npc.NpcMonsterVNum,
                                Type = type,
                                SubType = (byte) (int.Parse(currentLine[(5 * i) + 5]) + (1 * 10) + 1
                                    + (first > 0 ? 0 : 1)),
                                IsLevelScaled = Convert.ToBoolean(first % 4),
                                IsLevelDivided = (uint)(first > 0 ? first : -first) % 4 == 2,
                                FirstData = (short) ((first > 0 ? first : -first) / 4),
                                SecondData = (short) (int.Parse(currentLine[(5 * i) + 4]) / 4),
                                ThirdData = (short) (int.Parse(currentLine[(5 * i) + 6]) / 4)
                            };
                            monstercards.Add(itemCard);
                        }
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "BASIC")
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            var type = (byte) int.Parse(currentLine[(5 * i) + 2]);
                            if (type == 0)
                            {
                                continue;
                            }

                            var first = int.Parse(currentLine[(5 * i) + 5]);
                            var itemCard = new BCardDto
                            {
                                NpcMonsterVNum = npc.NpcMonsterVNum,
                                Type = type,
                                SubType =
                                    (byte) (((int.Parse(currentLine[(5 * i) + 6]) + 1) * 10) + 1 + (first > 0 ? 0 : 1)),
                                FirstData = (short) ((first > 0 ? first : -first) / 4),
                                SecondData = (short) (int.Parse(currentLine[(5 * i) + 4]) / 4),
                                ThirdData = (short) (int.Parse(currentLine[(5 * i) + 3]) / 4),
                                CastType = 1,
                                IsLevelScaled = false,
                                IsLevelDivided = false
                            };
                            monstercards.Add(itemCard);
                        }
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "ITEM")
                    {
                        if (DaoFactory.NpcMonsterDao.FirstOrDefault(s => s.NpcMonsterVNum.Equals(npc.NpcMonsterVNum))
                            == null)
                        {
                            npcs.Add(npc);
                            counter++;
                        }

                        for (var i = 2; i < currentLine.Length - 3; i += 3)
                        {
                            var vnum = Convert.ToInt16(currentLine[i]);
                            if (vnum == -1)
                            {
                                break;
                            }

                            if (DaoFactory.DropDao.Where(s => s.MonsterVNum == npc.NpcMonsterVNum)
                                .Count(s => s.VNum == vnum) != 0)
                            {
                                continue;
                            }

                            drops.Add(new DropDto
                            {
                                VNum = vnum,
                                Amount = Convert.ToInt32(currentLine[i + 2]),
                                MonsterVNum = npc.NpcMonsterVNum,
                                DropChance = Convert.ToInt32(currentLine[i + 1])
                            });
                        }

                        itemAreaBegin = false;
                    }
                }

                IEnumerable<NpcMonsterDto> npcMonsterDtos = npcs;
                IEnumerable<NpcMonsterSkillDto> npcMonsterSkillDtos = skills;
                IEnumerable<BCardDto> monsterBCardDtos = monstercards;

                DaoFactory.NpcMonsterDao.InsertOrUpdate(npcMonsterDtos);
                DaoFactory.NpcMonsterSkillDao.InsertOrUpdate(npcMonsterSkillDtos);
                DaoFactory.BCardDao.InsertOrUpdate(monsterBCardDtos);
                _logger.Information(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.NPCMONSTERS_PARSED),
                    counter));
            }

            IEnumerable<DropDto> dropDtos = drops;
            DaoFactory.DropDao.InsertOrUpdate(dropDtos);
        }
    }
}