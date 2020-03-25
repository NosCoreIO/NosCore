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
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using Serilog;

namespace NosCore.Parser.Parsers
{
    //    VNUM	{NpcMonsterVNum}
    //    NAME {NameI18NKey}

    //    LEVEL	{Level}
    //    RACE	{Race}	{RaceType}
    //    ATTRIB	{Element}	{ElementRate}	{FireResistance}	{WaterResistance}	{LightResistance}	{DarkResistance}
    //    HP/MP	{HP}	{MP}
    //    EXP	{XP}	{JXP}
    //    PREATT	{IsHostile}	{NoticeRange}	{Speed}	{RespawnTime}	400
    //    SETTING	0	0	-1	0	0	0
    //    ETC	8	1	0	0	0	0	0	0
    //    PETINFO	1	10	0	50
    //    EFF	200	0	0
    //    ZSKILL	0	1	3	2	12	0	0
    //    WINFO	0	0	0
    //    WEAPON	16	1	0	0	0	11	-20
    //    AINFO	0	0
    //    ARMOR	{CloseDefence}	{DistanceDefence}	{MagicDefence}	{DefenceDodge}	{DistanceDefenceDodge}
    //    SKILL	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
    //    PARTNER	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
    //    BASIC	0	0	4	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
    //    CARD	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
    //    MODE	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
    //    ITEM	2000	9000	1	16	800	1	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0	-1	0	0
    //#========================================================

    public class NpcMonsterParser
    {
        private string FileNpcId = $"{Path.DirectorySeparatorChar}monster.dat";
        private readonly IGenericDao<BCardDto> _bCardDao;
        private readonly IGenericDao<DropDto> _dropDao;
        private readonly ILogger _logger;
        private readonly IGenericDao<NpcMonsterDto> _npcMonsterDao;
        private readonly IGenericDao<NpcMonsterSkillDto> _npcMonsterSkillDao;
        private readonly IGenericDao<SkillDto> _skillDao;
        private readonly int[] _basicHp = new int[100];
        private readonly int[] _basicPrimaryMp = new int[100];
        private readonly int[] _basicSecondaryMp = new int[100];
        private readonly int[] _basicXp = new int[100];
        private readonly int[] _basicJXp = new int[100];
        private Dictionary<short, SkillDto>? _skilldb;
        private Dictionary<short, List<DropDto>>? _dropdb;

        public NpcMonsterParser(IGenericDao<SkillDto> skillDao, IGenericDao<BCardDto> bCardDao,
            IGenericDao<DropDto> dropDao, IGenericDao<NpcMonsterSkillDto> npcMonsterSkillDao,
            IGenericDao<NpcMonsterDto> npcMonsterDao, ILogger logger)
        {
            _skillDao = skillDao;
            _bCardDao = bCardDao;
            _dropDao = dropDao;
            _npcMonsterSkillDao = npcMonsterSkillDao;
            _npcMonsterDao = npcMonsterDao;
            _logger = logger;
            InitStats();
        }


        public void InsertNpcMonsters(string folder)
        {
            _skilldb = _skillDao.LoadAll().ToDictionary(x => x.SkillVNum, x => x);
            _dropdb = _dropDao.LoadAll().Where(x => x.MonsterVNum != null).GroupBy(x => x.MonsterVNum).ToDictionary(x => x.Key ?? 0, x => x.ToList());
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                {nameof(NpcMonsterDto.NpcMonsterVNum), chunk => Convert.ToInt16(chunk["VNUM"][0][2])},
                {nameof(NpcMonsterDto.NameI18NKey), chunk => chunk["NAME"][0][2]},
                {nameof(NpcMonsterDto.Level), chunk => Level(chunk)},
                {nameof(NpcMonsterDto.HeroXp), chunk => ImportHeroXp(chunk)},
                {nameof(NpcMonsterDto.Race), chunk => Convert.ToByte(chunk["RACE"][0][2])},
                {nameof(NpcMonsterDto.RaceType), chunk => Convert.ToByte(chunk["RACE"][0][3])},
                {nameof(NpcMonsterDto.Element), chunk => Convert.ToByte(chunk["ATTRIB"][0][2])},
                {nameof(NpcMonsterDto.ElementRate), chunk => Convert.ToInt16(chunk["ATTRIB"][0][3])},
                {nameof(NpcMonsterDto.FireResistance), chunk => Convert.ToInt16(chunk["ATTRIB"][0][4])},
                {nameof(NpcMonsterDto.WaterResistance), chunk => Convert.ToInt16(chunk["ATTRIB"][0][5])},
                {nameof(NpcMonsterDto.LightResistance), chunk => Convert.ToInt16(chunk["ATTRIB"][0][6])},
                {nameof(NpcMonsterDto.DarkResistance), chunk => Convert.ToInt16(chunk["ATTRIB"][0][7])},
                {nameof(NpcMonsterDto.MaxHp), chunk => Convert.ToInt32(chunk["HP/MP"][0][2]) + _basicHp[Level(chunk)]},
                {nameof(NpcMonsterDto.MaxMp), chunk => Convert.ToInt32(chunk["HP/MP"][0][3]) + Convert.ToByte(chunk["RACE"][0][2]) == 0 ? _basicPrimaryMp[Level(chunk)] : _basicSecondaryMp[Level(chunk)]},
                {nameof(NpcMonsterDto.Xp), chunk =>  Math.Abs(Convert.ToInt32(chunk["EXP"][0][2]) + _basicXp[Level(chunk)])},
                {nameof(NpcMonsterDto.JobXp), chunk => Convert.ToInt32(chunk["EXP"][0][3]) + _basicJXp[Level(chunk)]},
                {nameof(NpcMonsterDto.IsHostile), chunk => chunk["PREATT"][0][2] != "0" },
                {nameof(NpcMonsterDto.NoticeRange), chunk => Convert.ToByte(chunk["PREATT"][0][4])},
                {nameof(NpcMonsterDto.Speed), chunk => Convert.ToByte(chunk["PREATT"][0][5])},
                {nameof(NpcMonsterDto.RespawnTime), chunk => Convert.ToInt32(chunk["PREATT"][0][6])},
                {nameof(NpcMonsterDto.CloseDefence), chunk =>  Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 2 + 18)},
                {nameof(NpcMonsterDto.DistanceDefence), chunk =>  Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2])- 1) * 3 + 17)},
                {nameof(NpcMonsterDto.MagicDefence), chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 2 + 13)},
                {nameof(NpcMonsterDto.DefenceDodge), chunk =>  Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2])- 1) * 5 + 31)},
                {nameof(NpcMonsterDto.DistanceDefenceDodge), chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 5 + 31)},
                {nameof(NpcMonsterDto.AttackClass), chunk => Convert.ToByte(chunk["ZSKILL"][0][2])},
                {nameof(NpcMonsterDto.BasicRange), chunk => Convert.ToByte(chunk["ZSKILL"][0][3])},
                {nameof(NpcMonsterDto.BasicArea), chunk => Convert.ToByte(chunk["ZSKILL"][0][5])},
                {nameof(NpcMonsterDto.BasicCooldown), chunk => Convert.ToInt16(chunk["ZSKILL"][0][6])},
                {nameof(NpcMonsterDto.AttackUpgrade), chunk => Convert.ToByte(LoadUnknownData(chunk) == 1?chunk["WINFO"][0][2]:chunk["WINFO"][0][4])},
                {nameof(NpcMonsterDto.DefenceUpgrade), chunk => Convert.ToByte(LoadUnknownData(chunk) == 1? chunk["WINFO"][0][2]:chunk["AINFO"][0][3])},
                {nameof(NpcMonsterDto.BasicSkill), chunk => Convert.ToInt16(chunk["EFF"][0][2])},
                {nameof(NpcMonsterDto.VNumRequired), chunk => Convert.ToInt16(chunk["SETTING"][0][4] != "0" && ShouldLoadPetinfo(chunk) ? chunk["PETINFO"][0][2] : chunk["SETTING"][0][4])},
                {nameof(NpcMonsterDto.AmountRequired), chunk =>  Convert.ToByte(chunk["SETTING"][0][4] == "0" ? "1" : ShouldLoadPetinfo(chunk) ? chunk["PETINFO"][0][3] : "0")},
                {nameof(NpcMonsterDto.DamageMinimum), chunk => ImportDamageMinimum(chunk)},
                {nameof(NpcMonsterDto.DamageMaximum), chunk => ImportDamageMaximum(chunk)},
                {nameof(NpcMonsterDto.Concentrate), chunk => ImportConcentrate(chunk)},
                {nameof(NpcMonsterDto.CriticalChance), chunk => ImportCriticalChance(chunk)},
                {nameof(NpcMonsterDto.CriticalRate), chunk => ImportCriticalRate(chunk)},
                {nameof(NpcMonsterDto.NpcMonsterSkill), chunk => ImportNpcMonsterSkill(chunk)},
                {nameof(NpcMonsterDto.BCards), chunk => ImportBCards(chunk)},
                {nameof(NpcMonsterDto.Drop), chunk => ImportDrops(chunk)},
                {nameof(NpcMonsterDto.MonsterType), chunk => ImportMonsterType(chunk)},
                {nameof(NpcMonsterDto.NoAggresiveIcon), chunk => {
                        var unknowndata = LoadUnknownData(chunk);
                        return (unknowndata == -2147483616
                            || unknowndata ==  -2147483647
                            || unknowndata ==  -2147483646) && ((Convert.ToByte(chunk["RACE"][0][2]) == 8) && (Convert.ToByte(chunk["RACE"][0][3]) == 0));
                     }
                }
            };

            var genericParser = new GenericParser<NpcMonsterDto>(folder + FileNpcId,
                "#========================================================", 1, actionList, _logger);
            var monsters = genericParser.GetDtos().GroupBy(p => p.NpcMonsterVNum).Select(g => g.First()).ToList();
            _npcMonsterDao.InsertOrUpdate(monsters);
            _bCardDao.InsertOrUpdate(monsters.Where(s => s.BCards != null).SelectMany(s => s.BCards));
            _dropDao.InsertOrUpdate(monsters.Where(s => s.Drop != null).SelectMany(s => s.Drop));
            _npcMonsterSkillDao.InsertOrUpdate(monsters.Where(s => s.NpcMonsterSkill != null).SelectMany(s => s.NpcMonsterSkill));
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.NPCMONSTERS_PARSED), monsters.Count);
        }

        private short ImportDamageMinimum(Dictionary<string, string[][]> chunk)
        {
            return chunk["WEAPON"][0][3] switch
            {
                "1" => Convert.ToInt16((Convert.ToInt16(chunk["WEAPON"][0][2]) - 1) * 4 + 32 + Convert.ToInt16(chunk["WEAPON"][0][4]) + Math.Round(Convert.ToDecimal((Level(chunk) - 1) / 5))),
                "2" => Convert.ToInt16(Convert.ToInt16(chunk["WEAPON"][0][2]) * 6.5f + 23 + Convert.ToInt16(chunk["WEAPON"][0][4])),
                _ => (short)0,
            };
        }

        private short ImportDamageMaximum(Dictionary<string, string[][]> chunk)
        {
            return chunk["WEAPON"][0][3] switch
            {
                "1" => Convert.ToInt16((Convert.ToInt16(chunk["WEAPON"][0][2]) - 1) * 6 + 40 + Convert.ToInt16(chunk["WEAPON"][0][5]) - Math.Round(Convert.ToDecimal((Level(chunk) - 1) / 5))),
                "2" => Convert.ToInt16(Convert.ToInt16(chunk["WEAPON"][0][2]) * 6.5f + 23 + Convert.ToInt16(chunk["WEAPON"][0][4])),
                _ => (short)0,
            };

        }

        private short ImportConcentrate(Dictionary<string, string[][]> chunk)
        {
            return chunk["WEAPON"][0][3] switch
            {
                "1" => Convert.ToInt16((Convert.ToInt16(chunk["WEAPON"][0][2]) - 1) * 5 + 27 + Convert.ToInt16(chunk["WEAPON"][0][6])),
                "2" => Convert.ToInt16(70 + Convert.ToInt16(chunk["WEAPON"][0][6])),
                _ => (short)0,
            };
        }

        private byte ImportCriticalChance(Dictionary<string, string[][]> chunk)
        {
            return chunk["WEAPON"][0][3] switch
            {
                "1" => Convert.ToByte(4 + Convert.ToInt16(chunk["WEAPON"][0][7])),
                _ => (byte)0,
            };
        }

        private short ImportCriticalRate(Dictionary<string, string[][]> chunk)
        {
            return chunk["WEAPON"][0][3] switch
            {
                "1" => Convert.ToInt16(70 + Convert.ToInt16(chunk["WEAPON"][0][8])),
                _ => (short)0,
            };
        }

        private List<NpcMonsterSkillDto> ImportNpcMonsterSkill(Dictionary<string, string[][]> chunk)
        {
            var monstervnum = Convert.ToInt16(chunk["VNUM"][0][2]);
            var skills = new List<NpcMonsterSkillDto>();
            for (var i = 2; i < chunk["SKILL"][0].Length - 3; i += 3)
            {
                var vnum = short.Parse(chunk["SKILL"][0][i]);
                if ((vnum == -1) || (vnum == 0))
                {
                    break;
                }

                if (_skilldb?.ContainsKey(vnum) == false)
                {
                    continue;
                }

                skills.Add(new NpcMonsterSkillDto
                {
                    SkillVNum = vnum,
                    Rate = Convert.ToInt16(chunk["SKILL"][0][i + 1]),
                    NpcMonsterVNum = monstervnum
                });
            }

            return skills;
        }


        private List<BCardDto> ImportBCards(Dictionary<string, string[][]> chunk)
        {
            var monstercards = new List<BCardDto>();
            var monstervnum = Convert.ToInt16(chunk["VNUM"][0][2]);

            for (var i = 0; i < 4; i++)
            {
                var type = (byte)int.Parse(chunk["CARD"][0][5 * i + 2]);
                if ((type == 0) || (type == 255))
                {
                    continue;
                }

                var first = int.Parse(chunk["CARD"][0][5 * i + 3]);
                var itemCard = new BCardDto
                {
                    NpcMonsterVNum = monstervnum,
                    Type = type,
                    SubType = (byte)(int.Parse(chunk["CARD"][0][5 * i + 5]) + 1 * 10 + 1
                        + (first > 0 ? 0 : 1)),
                    IsLevelScaled = Convert.ToBoolean(first % 4),
                    IsLevelDivided = (uint)(first > 0 ? first : -first) % 4 == 2,
                    FirstData = (short)((first > 0 ? first : -first) / 4),
                    SecondData = (short)(int.Parse(chunk["CARD"][0][5 * i + 4]) / 4),
                    ThirdData = (short)(int.Parse(chunk["CARD"][0][5 * i + 6]) / 4)
                };
                monstercards.Add(itemCard);

                first = int.Parse(chunk["BASIC"][0][5 * i + 5]);
                itemCard = new BCardDto
                {
                    NpcMonsterVNum = monstervnum,
                    Type = type,
                    SubType =
                        (byte)((int.Parse(chunk["BASIC"][0][5 * i + 6]) + 1) * 10 + 1 + (first > 0 ? 0 : 1)),
                    FirstData = (short)((first > 0 ? first : -first) / 4),
                    SecondData = (short)(int.Parse(chunk["BASIC"][0][5 * i + 4]) / 4),
                    ThirdData = (short)(int.Parse(chunk["BASIC"][0][5 * i + 3]) / 4),
                    CastType = 1,
                    IsLevelScaled = false,
                    IsLevelDivided = false
                };
                monstercards.Add(itemCard);
            }

            return monstercards;
        }

        private List<DropDto> ImportDrops(Dictionary<string, string[][]> chunk)
        {
            var monstervnum = Convert.ToInt16(chunk["VNUM"][0][2]);
            var drops = new List<DropDto>();

            for (var i = 2; i < chunk["ITEM"][0].Length - 3; i += 3)
            {
                var vnum = Convert.ToInt16(chunk["ITEM"][0][i]);
                if (vnum == -1)
                {
                    break;
                }

                if (_dropdb?.ContainsKey(monstervnum) == true && (_dropdb[monstervnum].Count(s => s.VNum == vnum) != 0))
                {
                    continue;
                }

                drops.Add(new DropDto
                {
                    VNum = vnum,
                    Amount = Convert.ToInt32(chunk["ITEM"][0][i + 2]),
                    MonsterVNum = monstervnum,
                    DropChance = Convert.ToInt32(chunk["ITEM"][0][i + 1])
                });
            }

            return drops;
        }

        private MonsterType ImportMonsterType(Dictionary<string, string[][]> chunk)
        {
            var monstervnum = Convert.ToInt16(chunk["VNUM"][0][2]);
            var unknownData = LoadUnknownData(chunk);
            if (monstervnum >= 588 && monstervnum <= 607)
            {
                return MonsterType.Elite;
            }

            if (unknownData == -2147481593)
            {
                return MonsterType.Special;
            }

            return MonsterType.Unknown;
        }

        private void InitStats()
        {
            // basicHPLoad
            var baseHp = 138;
            var hPbasup = 18;
            for (var i = 0; i < 100; i++)
            {
                _basicHp[i] = baseHp;
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
            _basicPrimaryMp[0] = 10;
            _basicPrimaryMp[1] = 10;
            _basicPrimaryMp[2] = 15;

            var primaryBasup = 5;
            byte count = 0;
            var isStable = true;
            var isDouble = false;

            for (uint i = 3; i < 100; i++)
            {
                if (i % 10 == 1)
                {
                    _basicPrimaryMp[i] += _basicPrimaryMp[i - 1] + primaryBasup * 2;
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

                _basicPrimaryMp[i] = _basicPrimaryMp[i - (i % 10 == 2 ? 2 : 1)] + primaryBasup;
            }

            // Race == 2
            _basicSecondaryMp[0] = 60;
            _basicSecondaryMp[1] = 60;
            _basicSecondaryMp[2] = 78;

            var secondaryBasup = 18;
            var boostup = false;

            for (uint i = 3; i < 100; i++)
            {
                if (i % 10 == 1)
                {
                    _basicSecondaryMp[i] += _basicSecondaryMp[i - 1] + (int)i + 10;
                    continue;
                }

                boostup = !boostup;
                secondaryBasup += boostup ? 3 : 1;
                _basicSecondaryMp[i] = _basicSecondaryMp[i - (i % 10 == 2 ? 2 : 1)] + secondaryBasup;
            }

            // basicXPLoad
            for (var i = 0; i < 100; i++)
            {
                _basicXp[i] = i * 180;
            }

            // basicJXpLoad
            for (var i = 0; i < 100; i++)
            {
                _basicJXp[i] = 360;
            }

        }

        private int ImportHeroXp(Dictionary<string, string[][]> chunk)
        {
            return (Convert.ToInt32(chunk["VNUM"][0][2]) switch
            {
                2510 => 881,
                2501 => 881,
                2512 => 884,
                2502 => 884,
                2503 => 1013,
                2505 => 871,
                2506 => 765,
                2507 => 803,
                2508 => 825,
                2500 => 879,
                2509 => 879,
                2511 => 879,
                2513 => 1075,
                2515 => 3803,
                2516 => 836,
                2517 => 450,
                2518 => 911,
                2519 => 845,
                2520 => 3682,
                2521 => 401,
                2522 => 471,
                2523 => 328,
                2524 => 12718,
                2525 => 412,
                2526 => 11157,
                2527 => 18057,
                2530 => 28756,
                2559 => 1308,
                2560 => 1234,
                2561 => 1168,
                2562 => 959,
                2563 => 947,
                2564 => 952,
                2566 => 1097,
                2567 => 1096,
                2568 => 4340,
                2569 => 3534,
                2570 => 4343,
                2571 => 2205,
                2572 => 5632,
                2573 => 3756,
                _ => 0
            });
        }

        private byte Level(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(chunk["LEVEL"][0][2]);
        }

        private long LoadUnknownData(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt64(chunk["ETC"][0][2]);
        }
        private bool ShouldLoadPetinfo(Dictionary<string, string[][]> chunk)
        {
            var unknownData = LoadUnknownData(chunk);
            return !((unknownData != -2147481593) && (unknownData != -2147481599) && (unknownData != -1610610681));
        }

    }
}