using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.I18N;

namespace NosCore.Parser
{
    internal class SkillParser
    {
	    private readonly string fileSkillId = "\\Skill.dat";
        private string _folder;

        internal void InsertSkills(string folder)
        {
	        _folder = folder;
            List<SkillDTO> skills = new List<SkillDTO>();
            SkillDTO skill = new SkillDTO();
            List<ComboDTO> combo = new List<ComboDTO>();
            List<BCardDTO> skillCards = new List<BCardDTO>();
	        int counter = 0;

            using (StreamReader skillIdStream = new StreamReader(_folder+fileSkillId, Encoding.GetEncoding(1252)))
            {
	            string line;
	            while ((line = skillIdStream.ReadLine()) != null)
                {
                    string[] currentLine = line.Split('\t');

                    if (currentLine.Length > 2 && currentLine[1] == "VNUM")
                    {
                        skill = new SkillDTO
                        {
                            SkillVNum = short.Parse(currentLine[2])
                        };
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "NAME")
                    {
                        skill.Name = currentLine[2];
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "TYPE")
                    {
                        skill.SkillType = byte.Parse(currentLine[2]);
                        skill.CastId = short.Parse(currentLine[3]);
                        skill.Class = byte.Parse(currentLine[4]);
                        skill.Type = byte.Parse(currentLine[5]);
                        skill.Element = byte.Parse(currentLine[7]);
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "FCOMBO")
                    {
                        for (int i = 3; i < currentLine.Length - 4; i += 3)
                        {
                            ComboDTO comb = new ComboDTO
                            {
                                SkillVNum = skill.SkillVNum,
                                Hit = short.Parse(currentLine[i]),
                                Animation = short.Parse(currentLine[i + 1]),
                                Effect = short.Parse(currentLine[i + 2])
                            };

                            if (comb.Hit == 0 && comb.Animation == 0 && comb.Effect == 0)
                            {
                                continue;
                            }
                            if (DAOFactory.ComboDAO.FirstOrDefault(s => s.SkillVNum.Equals(comb.SkillVNum) && s.Hit.Equals(comb.Hit) && s.Effect.Equals(comb.Effect)) == null)
                            {
                                combo.Add(comb);
                            }
                        }
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "COST")
                    {
                        skill.CPCost = currentLine[2] == "-1" ? (byte)0 : byte.Parse(currentLine[2]);
                        skill.Price = int.Parse(currentLine[3]);
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "LEVEL")
                    {
                        skill.LevelMinimum = currentLine[2] != "-1" ? byte.Parse(currentLine[2]) : (byte)0;
                        if (skill.Class > 31)
                        {
                            SkillDTO firstskill = skills.FirstOrDefault(s => s.Class == skill.Class);
                            if (firstskill == null || skill.SkillVNum <= firstskill.SkillVNum + 10)
                            {
                                switch (skill.Class)
                                {
                                    case 8:
                                        switch (skills.Count(s => s.Class == skill.Class))
                                        {
                                            case 3:
                                                skill.LevelMinimum = 20;
                                                break;

                                            case 2:
                                                skill.LevelMinimum = 10;
                                                break;

                                            default:
                                                skill.LevelMinimum = 0;
                                                break;
                                        }
                                        break;

                                    case 9:
                                        switch (skills.Count(s => s.Class == skill.Class))
                                        {
                                            case 9:
                                                skill.LevelMinimum = 20;
                                                break;

                                            case 8:
                                                skill.LevelMinimum = 16;
                                                break;

                                            case 7:
                                                skill.LevelMinimum = 12;
                                                break;

                                            case 6:
                                                skill.LevelMinimum = 8;
                                                break;

                                            case 5:
                                                skill.LevelMinimum = 4;
                                                break;

                                            default:
                                                skill.LevelMinimum = 0;
                                                break;
                                        }
                                        break;

                                    case 16:
                                        switch (skills.Count(s => s.Class == skill.Class))
                                        {
                                            case 6:
                                                skill.LevelMinimum = 20;
                                                break;

                                            case 5:
                                                skill.LevelMinimum = 15;
                                                break;

                                            case 4:
                                                skill.LevelMinimum = 10;
                                                break;

                                            case 3:
                                                skill.LevelMinimum = 5;
                                                break;

                                            case 2:
                                                skill.LevelMinimum = 3;
                                                break;

                                            default:
                                                skill.LevelMinimum = 0;
                                                break;
                                        }
                                        break;

                                    default:
                                        switch (skills.Count(s => s.Class == skill.Class))
                                        {
                                            case 10:
                                                skill.LevelMinimum = 20;
                                                break;

                                            case 9:
                                                skill.LevelMinimum = 16;
                                                break;

                                            case 8:
                                                skill.LevelMinimum = 12;
                                                break;

                                            case 7:
                                                skill.LevelMinimum = 8;
                                                break;

                                            case 6:
                                                skill.LevelMinimum = 4;
                                                break;

                                            default:
                                                skill.LevelMinimum = 0;
                                                break;
                                        }
                                        break;
                                }
                            }
                        }
                        skill.MinimumAdventurerLevel = currentLine[3] != "-1" ? byte.Parse(currentLine[3]) : (byte)0;
                        skill.MinimumSwordmanLevel = currentLine[4] != "-1" ? byte.Parse(currentLine[4]) : (byte)0;
                        skill.MinimumArcherLevel = currentLine[5] != "-1" ? byte.Parse(currentLine[5]) : (byte)0;
                        skill.MinimumMagicianLevel = currentLine[6] != "-1" ? byte.Parse(currentLine[6]) : (byte)0;
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "EFFECT")
                    {
                        skill.CastEffect = short.Parse(currentLine[3]);
                        skill.CastAnimation = short.Parse(currentLine[4]);
                        skill.Effect = short.Parse(currentLine[5]);
                        skill.AttackAnimation = short.Parse(currentLine[6]);
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "TARGET")
                    {
                        skill.TargetType = byte.Parse(currentLine[2]);
                        skill.HitType = byte.Parse(currentLine[3]);
                        skill.Range = byte.Parse(currentLine[4]);
                        skill.TargetRange = byte.Parse(currentLine[5]);
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "DATA")
                    {
                        skill.UpgradeSkill = short.Parse(currentLine[2]);
                        skill.UpgradeType = short.Parse(currentLine[3]);
                        skill.CastTime = short.Parse(currentLine[6]);
                        skill.Cooldown = short.Parse(currentLine[7]);
                        skill.MpCost = short.Parse(currentLine[10]);
                        skill.ItemVNum = short.Parse(currentLine[12]);
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "BASIC")
                    {
                        byte type = (byte)int.Parse(currentLine[3]);
                        if (type == 0 || type == 255)
                        {
                            continue;
                        }
                        int first = int.Parse(currentLine[5]);
                        BCardDTO itemCard = new BCardDTO
                        {
                            SkillVNum = skill.SkillVNum,
                            Type = type,
                            SubType = (byte)((int.Parse(currentLine[4]) + 1) * 10 + 1 + (first < 0 ? 1 : 0)),
                            IsLevelScaled = Convert.ToBoolean(first % 4),
                            IsLevelDivided = (first % 4) == 2,
                            FirstData = (short)(first > 0 ? first : -first / 4),
                            SecondData = (short)(int.Parse(currentLine[6]) / 4),
                            ThirdData = (short)(int.Parse(currentLine[7]) / 4),
                        };
                        skillCards.Add(itemCard);
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "FCOMBO")
                    {
                        // investigate
                        /*
                        if (currentLine[2] == "1")
                        {
                            combo.FirstActivationHit = byte.Parse(currentLine[3]);
                            combo.FirstComboAttackAnimation = short.Parse(currentLine[4]);
                            combo.FirstComboEffect = short.Parse(currentLine[5]);
                            combo.SecondActivationHit = byte.Parse(currentLine[3]);
                            combo.SecondComboAttackAnimation = short.Parse(currentLine[4]);
                            combo.SecondComboEffect = short.Parse(currentLine[5]);
                            combo.ThirdActivationHit = byte.Parse(currentLine[3]);
                            combo.ThirdComboAttackAnimation = short.Parse(currentLine[4]);
                            combo.ThirdComboEffect = short.Parse(currentLine[5]);
                            combo.FourthActivationHit = byte.Parse(currentLine[3]);
                            combo.FourthComboAttackAnimation = short.Parse(currentLine[4]);
                            combo.FourthComboEffect = short.Parse(currentLine[5]);
                            combo.FifthActivationHit = byte.Parse(currentLine[3]);
                            combo.FifthComboAttackAnimation = short.Parse(currentLine[4]);
                            combo.FifthComboEffect = short.Parse(currentLine[5]);
                        }
                        */
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "CELL")
                    {
                        // investigate
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "Z_DESC")
                    {
                        // investigate
                        SkillDTO skill1 = skill;
                        if (DAOFactory.SkillDAO.FirstOrDefault(s => s.SkillVNum.Equals(skill1.SkillVNum)) != null)
                        {
                            continue;
                        }
                        skills.Add(skill);
                        counter++;
                    }
                }

                IEnumerable<SkillDTO> skillDtos = skills;
                IEnumerable<ComboDTO> comboDtos = combo;
                IEnumerable<BCardDTO> bCardDtos = skillCards;


                DAOFactory.SkillDAO.InsertOrUpdate(skillDtos);
                DAOFactory.ComboDAO.InsertOrUpdate(comboDtos);
                DAOFactory.BCardDAO.InsertOrUpdate(bCardDtos);

                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SKILLS_PARSED), counter));
            }
        }
    }
}