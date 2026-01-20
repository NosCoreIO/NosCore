//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
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
        private readonly string _fileNpcId = $"{Path.DirectorySeparatorChar}monster.dat";
        private readonly IDao<BCardDto, short> _bCardDao;
        private readonly IDao<DropDto, short> _dropDao;
        private readonly ILogger _logger;
        private readonly IDao<NpcMonsterDto, short> _npcMonsterDao;
        private readonly IDao<NpcMonsterSkillDto, long> _npcMonsterSkillDao;
        private readonly IDao<SkillDto, short> _skillDao;
        private readonly int[] _basicHp = new int[100];
        private readonly int[] _basicPrimaryMp = new int[100];
        private readonly int[] _basicSecondaryMp = new int[100];
        private Dictionary<short, SkillDto>? _skilldb;
        private Dictionary<short, List<DropDto>>? _dropdb;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public NpcMonsterParser(IDao<SkillDto, short> skillDao, IDao<BCardDto, short> bCardDao,
            IDao<DropDto, short> dropDao, IDao<NpcMonsterSkillDto, long> npcMonsterSkillDao,
            IDao<NpcMonsterDto, short> npcMonsterDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _skillDao = skillDao;
            _bCardDao = bCardDao;
            _dropDao = dropDao;
            _npcMonsterSkillDao = npcMonsterSkillDao;
            _npcMonsterDao = npcMonsterDao;
            _logger = logger;
            _logLanguage = logLanguage;
            InitStats();
        }


        public async Task InsertNpcMonstersAsync(string folder)
        {
            _skilldb = _skillDao.LoadAll().ToDictionary(x => x.SkillVNum, x => x);
            _dropdb = _dropDao.LoadAll().Where(x => x.MonsterVNum != null).GroupBy(x => x.MonsterVNum).ToDictionary(x => x.Key ?? 0, x => x.ToList());
            var parser = FluentParserBuilder<NpcMonsterDto>.Create(folder + _fileNpcId, "#========================================================", 1)
                .Field(x => x.NpcMonsterVNum, chunk => Convert.ToInt16(chunk["VNUM"][0][2]))
                .Field(x => x.NameI18NKey, chunk => chunk["NAME"][0][2])
                .Field(x => x.Level, chunk => Level(chunk))
                .Field(x => x.HeroXp, chunk => ImportXp(chunk) / 25)
                .Field(x => x.Race, chunk => Convert.ToByte(chunk["RACE"][0][2]))
                .Field(x => x.RaceType, chunk => Convert.ToByte(chunk["RACE"][0][3]))
                .Field(x => x.Element, chunk => Convert.ToByte(chunk["ATTRIB"][0][2]))
                .Field(x => x.ElementRate, chunk => Convert.ToInt16(chunk["ATTRIB"][0][3]))
                .Field(x => x.FireResistance, chunk => Convert.ToInt16(chunk["ATTRIB"][0][4]))
                .Field(x => x.WaterResistance, chunk => Convert.ToInt16(chunk["ATTRIB"][0][5]))
                .Field(x => x.LightResistance, chunk => Convert.ToInt16(chunk["ATTRIB"][0][6]))
                .Field(x => x.DarkResistance, chunk => Convert.ToInt16(chunk["ATTRIB"][0][7]))
                .Field(x => x.MaxHp, chunk => Convert.ToInt32(chunk["HP/MP"][0][2]) + _basicHp[Level(chunk)])
                .Field(x => x.MaxMp, chunk => Convert.ToInt32(chunk["HP/MP"][0][3]) + (Convert.ToByte(chunk["RACE"][0][2]) == 0 ? _basicPrimaryMp[Level(chunk)] : _basicSecondaryMp[Level(chunk)]))
                .Field(x => x.Xp, chunk => ImportXp(chunk))
                .Field(x => x.JobXp, chunk => ImportJxp(chunk))
                .Field(x => x.IsHostile, chunk => chunk["PREATT"][0][2] != "0")
                .Field(x => x.NoticeRange, chunk => Convert.ToByte(chunk["PREATT"][0][4]))
                .Field(x => x.Speed, chunk => Convert.ToByte(chunk["PREATT"][0][5]))
                .Field(x => x.RespawnTime, chunk => Convert.ToInt32(chunk["PREATT"][0][6]))
                .Field(x => x.CloseDefence, chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 2 + 18))
                .Field(x => x.DistanceDefence, chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 3 + 17))
                .Field(x => x.MagicDefence, chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 2 + 13))
                .Field(x => x.DefenceDodge, chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 5 + 31))
                .Field(x => x.DistanceDefenceDodge, chunk => Convert.ToInt16((Convert.ToInt16(chunk["ARMOR"][0][2]) - 1) * 5 + 31))
                .Field(x => x.AttackClass, chunk => Convert.ToByte(chunk["ZSKILL"][0][2]))
                .Field(x => x.BasicRange, chunk => Convert.ToByte(chunk["ZSKILL"][0][3]))
                .Field(x => x.BasicArea, chunk => Convert.ToByte(chunk["ZSKILL"][0][5]))
                .Field(x => x.BasicCooldown, chunk => Convert.ToInt16(chunk["ZSKILL"][0][6]))
                .Field(x => x.AttackUpgrade, chunk => Convert.ToByte(LoadUnknownData(chunk) == 1 ? chunk["WINFO"][0][2] : chunk["WINFO"][0][4]))
                .Field(x => x.DefenceUpgrade, chunk => Convert.ToByte(LoadUnknownData(chunk) == 1 ? chunk["WINFO"][0][2] : chunk["AINFO"][0][3]))
                .Field(x => x.BasicSkill, chunk => Convert.ToInt16(chunk["EFF"][0][2]))
                .Field(x => x.VNumRequired, chunk => Convert.ToInt16(chunk["SETTING"][0][4] != "0" && ShouldLoadPetinfo(chunk) ? chunk["PETINFO"][0][2] : chunk["SETTING"][0][4]))
                .Field(x => x.AmountRequired, chunk => Convert.ToByte(chunk["SETTING"][0][4] == "0" ? "1" : ShouldLoadPetinfo(chunk) ? chunk["PETINFO"][0][3] : "0"))
                .Field(x => x.DamageMinimum, chunk => ImportDamageMinimum(chunk))
                .Field(x => x.DamageMaximum, chunk => ImportDamageMaximum(chunk))
                .Field(x => x.Concentrate, chunk => ImportConcentrate(chunk))
                .Field(x => x.CriticalChance, chunk => ImportCriticalChance(chunk))
                .Field(x => x.CriticalRate, chunk => ImportCriticalRate(chunk))
                .Field(x => x.NpcMonsterSkill, chunk => ImportNpcMonsterSkill(chunk))
                .Field(x => x.BCards, chunk => ImportBCards(chunk))
                .Field(x => x.Drop, chunk => ImportDrops(chunk))
                .Field(x => x.MonsterType, chunk => ImportMonsterType(chunk))
                .Field(x => x.NoAggresiveIcon, chunk =>
                {
                    var unknowndata = LoadUnknownData(chunk);
                    return (unknowndata == -2147483616 || unknowndata == -2147483647 || unknowndata == -2147483646)
                        && (Convert.ToByte(chunk["RACE"][0][2]) == 8) && (Convert.ToByte(chunk["RACE"][0][3]) == 0);
                })
                .Build(_logger, _logLanguage);
            var monsters = (await parser.GetDtosAsync()).GroupBy(p => p.NpcMonsterVNum).Select(g => g.First()).ToList();
            await _npcMonsterDao.TryInsertOrUpdateAsync(monsters);
            await _bCardDao.TryInsertOrUpdateAsync(monsters.Where(s => s.BCards != null).SelectMany(s => s.BCards));
            await _dropDao.TryInsertOrUpdateAsync(monsters.Where(s => s.Drop != null).SelectMany(s => s.Drop));
            await _npcMonsterSkillDao.TryInsertOrUpdateAsync(monsters.Where(s => s.NpcMonsterSkill != null).SelectMany(s => s.NpcMonsterSkill));
            _logger.Information(_logLanguage[LogLanguageKey.NPCMONSTERS_PARSED], monsters.Count);
        }

        private int ImportJxp(Dictionary<string, string[][]> chunk)
        {
            var value = Convert.ToInt32(chunk["EXP"][0][3]);
            if (value < 1)
            {
                value *= -1;
            }

            if (Level(chunk) < 61)
            {
                return value + 120;
            }

            return value + 105;
        }

        private int ImportXp(Dictionary<string, string[][]> chunk)
        {
            var value = Convert.ToInt32(chunk["EXP"][0][2]);
            if (value < 1)
            {
                value *= -1;
            }

            if (Level(chunk) >= 19)
            {
                return Math.Abs((Level(chunk) * 60) + Level(chunk) * 10 + value);
            }

            return Math.Abs((Level(chunk) * 60) + value);
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
