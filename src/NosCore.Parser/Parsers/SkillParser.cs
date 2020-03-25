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
    public class SkillParser
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
        private string FileCardDat = $"{Path.DirectorySeparatorChar}Skill.dat";
        private readonly IGenericDao<BCardDto> _bCardDao;
        private readonly IGenericDao<ComboDto> _comboDao;
        private readonly IGenericDao<SkillDto> _skillDao;
        private readonly ILogger _logger;

        public SkillParser(IGenericDao<BCardDto> bCardDao, IGenericDao<ComboDto> comboDao,
            IGenericDao<SkillDto> skillDao, ILogger logger)
        {
            _bCardDao = bCardDao;
            _comboDao = comboDao;
            _skillDao = skillDao;
            _logger = logger;
        }

        public void InsertSkills(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object>>
            {
                {nameof(SkillDto.SkillVNum), chunk => Convert.ToInt16(chunk["VNUM"][0][2])},
                {nameof(SkillDto.NameI18NKey), chunk => chunk["NAME"][0][2]},
                {nameof(SkillDto.SkillType), chunk => Convert.ToByte(chunk["TYPE"][0][2])},
                {nameof(SkillDto.CastId), chunk => Convert.ToInt16(chunk["TYPE"][0][3])},
                {nameof(SkillDto.Class), chunk => Convert.ToByte(chunk["TYPE"][0][4])},
                {nameof(SkillDto.Type), chunk => Convert.ToByte(chunk["TYPE"][0][5])},
                {nameof(SkillDto.Element), chunk => Convert.ToByte(chunk["TYPE"][0][7])},
                {nameof(SkillDto.Combo), AddCombos},
                {nameof(SkillDto.CpCost), chunk => chunk["COST"][0][2] == "-1" ? (byte)0 : byte.Parse(chunk["COST"][0][2])},
                {nameof(SkillDto.Price), chunk => Convert.ToInt32(chunk["COST"][0][3])},
                {nameof(SkillDto.CastEffect), chunk => Convert.ToInt16(chunk["EFFECT"][0][3])},
                {nameof(SkillDto.CastAnimation), chunk => Convert.ToInt16(chunk["EFFECT"][0][4])},
                {nameof(SkillDto.Effect), chunk => Convert.ToInt16(chunk["EFFECT"][0][5])},
                {nameof(SkillDto.AttackAnimation), chunk => Convert.ToInt16(chunk["EFFECT"][0][6])},
                {nameof(SkillDto.TargetType), chunk => Convert.ToByte(chunk["TARGET"][0][2])},
                {nameof(SkillDto.HitType), chunk => Convert.ToByte(chunk["TARGET"][0][3])},
                {nameof(SkillDto.Range), chunk => Convert.ToByte(chunk["TARGET"][0][4])},
                {nameof(SkillDto.TargetRange), chunk => Convert.ToByte(chunk["TARGET"][0][5])},
                {nameof(SkillDto.UpgradeSkill), chunk => Convert.ToInt16(chunk["DATA"][0][2])},
                {nameof(SkillDto.UpgradeType), chunk => Convert.ToInt16(chunk["DATA"][0][3])},
                {nameof(SkillDto.CastTime), chunk => Convert.ToInt16(chunk["DATA"][0][6])},
                {nameof(SkillDto.Cooldown), chunk => Convert.ToInt16(chunk["DATA"][0][7])},
                {nameof(SkillDto.MpCost), chunk => Convert.ToInt16(chunk["DATA"][0][10])},
                {nameof(SkillDto.ItemVNum), chunk => Convert.ToInt16(chunk["DATA"][0][12])},
                {nameof(SkillDto.BCards), AddBCards},
                {nameof(SkillDto.MinimumAdventurerLevel), chunk => chunk["LEVEL"][0][3] != "-1" ? byte.Parse(chunk["LEVEL"][0][3]) : (byte)0},
                {nameof(SkillDto.MinimumSwordmanLevel), chunk => chunk["LEVEL"][0][4] != "-1" ? byte.Parse(chunk["LEVEL"][0][4]) : (byte)0},
                {nameof(SkillDto.MinimumArcherLevel), chunk => chunk["LEVEL"][0][5] != "-1" ? byte.Parse(chunk["LEVEL"][0][5]) : (byte)0},
                {nameof(SkillDto.MinimumMagicianLevel), chunk => chunk["LEVEL"][0][6] != "-1" ? byte.Parse(chunk["LEVEL"][0][6]) : (byte)0},
                {nameof(SkillDto.LevelMinimum), chunk => chunk["LEVEL"][0][2] != "-1" ? byte.Parse(chunk["LEVEL"][0][2]) : (byte)0 },
            };
            var genericParser = new GenericParser<SkillDto>(folder + FileCardDat,
                "#=========================================================", 1, actionList, _logger);
            var skills = genericParser.GetDtos();

            foreach (var skill in skills.Where(s => s.Class > 31))
            {
                var firstskill = skills.Find(s => s.Class == skill.Class);
                var skillscount = skills.Count(s => s.Class == skill.Class);
                if ((firstskill == null) || (skill.SkillVNum <= firstskill.SkillVNum + 10))
                {
                    skill.LevelMinimum = (byte)(skill.Class switch
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
                    });

                }
            }

            _skillDao.InsertOrUpdate(skills);
            _comboDao.InsertOrUpdate(skills.Where(s => s.Combo != null).SelectMany(s => s.Combo));
            _bCardDao.InsertOrUpdate(skills.Where(s => s.BCards != null).SelectMany(s => s.BCards));

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SKILLS_PARSED), skills.Count);
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
            };
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
            };
            return list;
        }
    }
}