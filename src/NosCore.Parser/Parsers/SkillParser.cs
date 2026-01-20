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
    public class SkillParser(IDao<BCardDto, short> bCardDao, IDao<ComboDto, int> comboDao,
        IDao<SkillDto, short> skillDao, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        //  VNUM    {VNum}  
        //	NAME    {Name}

        //  TYPE	0	0	0	0	0	0
        //	COST	0	0	0
        //	LEVEL	0	0	0	0	0
        //	EFFECT	0	0	0	0	0	0	0	0	0
        //	TARGET	0	0	0	0	0
        //	DATA	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
        //	BASIC	0	0	0	0	0	0
        //	BASIC	1	0	0	0	0	0
        //	BASIC	2	0	0	0	0	0
        //	BASIC	3	0	0	0	0	0
        //	BASIC	4	0	0	0	0	0
        //	FCOMBO	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
        //	CELL	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
        //	Z_DESC	0

        //#=========================================================
        private readonly string _fileCardDat = $"{Path.DirectorySeparatorChar}Skill.dat";

        public async Task InsertSkillsAsync(string folder)
        {
            var parser = FluentParserBuilder<SkillDto>.Create(folder + _fileCardDat, "#=========================================================", 1)
                .Field(x => x.SkillVNum, chunk => Convert.ToInt16(chunk["VNUM"][0][2]))
                .Field(x => x.NameI18NKey, chunk => chunk["NAME"][0][2])
                .Field(x => x.SkillType, chunk => Convert.ToByte(chunk["TYPE"][0][2]))
                .Field(x => x.CastId, chunk => Convert.ToInt16(chunk["TYPE"][0][3]))
                .Field(x => x.Class, chunk => Convert.ToByte(chunk["TYPE"][0][4]))
                .Field(x => x.Type, chunk => Convert.ToByte(chunk["TYPE"][0][5]))
                .Field(x => x.Element, chunk => Convert.ToByte(chunk["TYPE"][0][7]))
                .Field(x => x.Combo, chunk => AddCombos(chunk))
                .Field(x => x.CpCost, chunk => chunk["COST"][0][2] == "-1" ? (byte)0 : byte.Parse(chunk["COST"][0][2]))
                .Field(x => x.Price, chunk => Convert.ToInt32(chunk["COST"][0][3]))
                .Field(x => x.CastEffect, chunk => Convert.ToInt16(chunk["EFFECT"][0][3]))
                .Field(x => x.CastAnimation, chunk => Convert.ToInt16(chunk["EFFECT"][0][4]))
                .Field(x => x.Effect, chunk => Convert.ToInt16(chunk["EFFECT"][0][5]))
                .Field(x => x.AttackAnimation, chunk => Convert.ToInt16(chunk["EFFECT"][0][6]))
                .Field(x => x.TargetType, chunk => Convert.ToByte(chunk["TARGET"][0][2]))
                .Field(x => x.HitType, chunk => Convert.ToByte(chunk["TARGET"][0][3]))
                .Field(x => x.Range, chunk => Convert.ToByte(chunk["TARGET"][0][4]))
                .Field(x => x.TargetRange, chunk => Convert.ToByte(chunk["TARGET"][0][5]))
                .Field(x => x.UpgradeSkill, chunk => Convert.ToInt16(chunk["DATA"][0][2]))
                .Field(x => x.UpgradeType, chunk => Convert.ToInt16(chunk["DATA"][0][3]))
                .Field(x => x.CastTime, chunk => Convert.ToInt16(chunk["DATA"][0][6]))
                .Field(x => x.Cooldown, chunk => Convert.ToInt16(chunk["DATA"][0][7]))
                .Field(x => x.MpCost, chunk => Convert.ToInt16(chunk["DATA"][0][10]))
                .Field(x => x.ItemVNum, chunk => Convert.ToInt16(chunk["DATA"][0][12]))
                .Field(x => x.BCards, chunk => AddBCards(chunk))
                .Field(x => x.MinimumAdventurerLevel, chunk => chunk["LEVEL"][0][3] != "-1" ? byte.Parse(chunk["LEVEL"][0][3]) : (byte)0)
                .Field(x => x.MinimumSwordmanLevel, chunk => chunk["LEVEL"][0][4] != "-1" ? byte.Parse(chunk["LEVEL"][0][4]) : (byte)0)
                .Field(x => x.MinimumArcherLevel, chunk => chunk["LEVEL"][0][5] != "-1" ? byte.Parse(chunk["LEVEL"][0][5]) : (byte)0)
                .Field(x => x.MinimumMagicianLevel, chunk => chunk["LEVEL"][0][6] != "-1" ? byte.Parse(chunk["LEVEL"][0][6]) : (byte)0)
                .Field(x => x.LevelMinimum, chunk => chunk["LEVEL"][0][2] != "-1" ? byte.Parse(chunk["LEVEL"][0][2]) : (byte)0)
                .Build(logger, logLanguage);
            var skills = await parser.GetDtosAsync();

            foreach (var skill in skills.Where(s => s.Class > 31))
            {
                var firstskill = skills.Find(s => s.Class == skill.Class);
                var skillscount = skills.Count(s => s.Class == skill.Class);
                if ((firstskill == null) || (skill.SkillVNum <= firstskill.SkillVNum + 10))
                {
                    skill.LevelMinimum = skill.Class switch
                    {
                        8 => (byte)(skillscount - 1 * 10),
                        9 => (byte)(skillscount - 4 * 4),
                        16 => (byte)skillscount switch
                        {
                            6 => 20,
                            5 => 15,
                            4 => 10,
                            3 => 5,
                            2 => 3,
                            _ => 0
                        },
                        _ => (byte)(skillscount - 5 * 4)
                    };

                }
            }

            await skillDao.TryInsertOrUpdateAsync(skills);
            await comboDao.TryInsertOrUpdateAsync(skills.Where(s => s.Combo != null).SelectMany(s => s.Combo));
            await bCardDao.TryInsertOrUpdateAsync(skills.Where(s => s.BCards != null).SelectMany(s => s.BCards));

            logger.Information(logLanguage[LogLanguageKey.SKILLS_PARSED], skills.Count);
        }

        private List<BCardDto> AddBCards(Dictionary<string, string[][]> chunks)
        {
            var list = new List<BCardDto>();
            for (var j = 0; j < chunks["BASIC"].Length; j++)
            {
                var type = (byte)int.Parse(chunks["BASIC"][j][3]);
                if ((type == 0) || (type == 255))
                {
                    continue;
                }

                var first = int.Parse(chunks["BASIC"][j][5]);
                var comb = new BCardDto
                {
                    SkillVNum = Convert.ToInt16(chunks["VNUM"][0][2]),
                    Type = type,
                    SubType = (byte)((int.Parse(chunks["BASIC"][j][4]) + 1) * 10 + 1 + (first < 0 ? 1 : 0)),
                    IsLevelScaled = Convert.ToBoolean((uint)(first < 0 ? 0 : first) % 4),
                    IsLevelDivided = (uint)(first < 0 ? 0 : first) % 4 == 2,
                    FirstData = (short)(first > 0 ? first : -first / 4),
                    SecondData = (short)(int.Parse(chunks["BASIC"][j][6]) / 4),
                    ThirdData = (short)(int.Parse(chunks["BASIC"][j][7]) / 4)
                };
                list.Add(comb);
            }

            return list;
        }

        private List<ComboDto> AddCombos(Dictionary<string, string[][]> chunks)
        {
            var list = new List<ComboDto>();
            for (var j = 0; j < 5; j++)
            {
                var comb = new ComboDto
                {
                    SkillVNum = Convert.ToInt16(chunks["VNUM"][0][2]),
                    Hit = short.Parse(chunks["FCOMBO"][0][j * 3 + 2]),
                    Animation = short.Parse(chunks["FCOMBO"][0][j * 3 + 3]),
                    Effect = short.Parse(chunks["FCOMBO"][0][j * 3 + 4])
                };
                if ((comb.Hit == 0) && (comb.Animation == 0) && (comb.Effect == 0))
                {
                    continue;
                }
                list.Add(comb);
            }

            return list;
        }
    }
}
