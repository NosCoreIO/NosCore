using System.Collections.Generic;
using System.IO;
using NosCore.Data;
using NosCore.Core.Encryption;
using NosCore.Domain;
using System.Text;
using NosCore.DAL;
using NosCore.Domain.Account;
using NosCore.Parser.Parsers;
using NosCore.Configuration;

namespace NosCore.Parser
{
    public class ImportFactory
    {
        #region Members

        private readonly string _folder;
        private readonly List<string[]> _packetList = new List<string[]>();

        private readonly MapParser _mapParser;
        private readonly MapNpcParser _mapNpcParser = new MapNpcParser();
        private readonly CardParser _cardParser = new CardParser();
        private readonly ItemParser _itemParser = new ItemParser();
        private readonly ParserConfiguration configuration;

        public ImportFactory(string folder, ParserConfiguration conf)
        {
            configuration = conf;
            _folder = folder;
            _mapParser = new MapParser(conf); //TODO add dependency injection in importer
        }
        
        public void ImportAccounts()
        {
            AccountDTO acc1 = new AccountDTO
            {
                Authority = AuthorityType.GameMaster,
                Name = "admin",
                Password = EncryptionHelper.Sha512("test")
            };
            DAOFactory.AccountDAO.InsertOrUpdate(ref acc1);

            AccountDTO acc2 = new AccountDTO
            {
                Authority = AuthorityType.User,
                Name = "test",
                Password = EncryptionHelper.Sha512("test")
            };
            DAOFactory.AccountDAO.InsertOrUpdate(ref acc2);
        }

        public void ImportCards()
        {
           _cardParser.InsertCards();
        }

        public void ImportMapNpcs()
        {
            _mapNpcParser.InsertMapNpcs(_packetList);
        }

        public void ImportMaps()
        {
            _mapParser.InsertOrUpdateMaps(_folder, _packetList);
        }

        public void ImportQuests()
        {

        }

        public void ImportMapType()
        {

        }

        public void ImportMapTypeMap()
        {

        }

        public void ImportMonsters()
        {

        }

        public void ImportNpcMonsterData()
        {
            /*
            foreach (string[] currentPacket in _packetList.Where(o => o[0].Equals("e_info") && o[1].Equals("10")))
            {
                if (currentPacket.Length <= 25)
                {
                    continue;
                }
                NpcMonsterDTO npcMonster = DaoFactory.NpcMonsterDao.LoadByVNum(short.Parse(currentPacket[2]));
                if (npcMonster == null)
                {
                    continue;
                }
                npcMonster.AttackClass = byte.Parse(currentPacket[5]);
                npcMonster.AttackUpgrade = byte.Parse(currentPacket[7]);
                npcMonster.DamageMinimum = short.Parse(currentPacket[8]);
                npcMonster.DamageMaximum = short.Parse(currentPacket[9]);
                npcMonster.Concentrate = short.Parse(currentPacket[10]);
                npcMonster.CriticalChance = byte.Parse(currentPacket[11]);
                npcMonster.CriticalRate = short.Parse(currentPacket[12]);
                npcMonster.DefenceUpgrade = byte.Parse(currentPacket[13]);
                npcMonster.CloseDefence = short.Parse(currentPacket[14]);
                npcMonster.DefenceDodge = short.Parse(currentPacket[15]);
                npcMonster.DistanceDefence = short.Parse(currentPacket[16]);
                npcMonster.DistanceDefenceDodge = short.Parse(currentPacket[17]);
                npcMonster.MagicDefence = short.Parse(currentPacket[18]);
                npcMonster.FireResistance = sbyte.Parse(currentPacket[19]);
                npcMonster.WaterResistance = sbyte.Parse(currentPacket[20]);
                npcMonster.LightResistance = sbyte.Parse(currentPacket[21]);
                npcMonster.DarkResistance = sbyte.Parse(currentPacket[22]);

                DaoFactory.NpcMonsterDao.InsertOrUpdate(ref npcMonster);
            }
            */
        }

        public void ImportNpcMonsters()
        {
            /*
            int[] basicHp = new int[100];
            int[] basicPrimaryMp = new int[100];
            int[] basicSecondaryMp = new int[100];
            int[] basicXp = new int[100];
            int[] basicJXp = new int[100];

            // basicHpLoad
            int baseHp = 138;
            int HPbasup = 18;
            for (int i = 0; i < 100; i++)
            {
                basicHp[i] = baseHp;
                HPbasup++;
                baseHp += HPbasup;

                if (i == 37)
                {
                    baseHp = 1765;
                    HPbasup = 65;
                }
                if (i < 41)
                {
                    continue;
                }
                if (((99 - i) % 8) == 0)
                {
                    HPbasup++;
                }
            }

            //Race == 0
            basicPrimaryMp[0] = 10;
            basicPrimaryMp[1] = 10;
            basicPrimaryMp[2] = 15;

            int primaryBasup = 5;
            byte count = 0;
            bool isStable = true;
            bool isDouble = false;

            for (int i = 3; i < 100; i++)
            {
                if (i % 10 == 1)
                {
                    basicPrimaryMp[i] += basicPrimaryMp[i - 1] + primaryBasup * 2;
                    continue;
                }
                if (!isStable)
                {
                    primaryBasup++;
                    count++;

                    if (count == 2)
                    {
                        if (isDouble)
                        { isDouble = false; }
                        else
                        { isStable = true; isDouble = true; count = 0; }
                    }

                    if (count == 4)
                    { isStable = true; count = 0; }
                }
                else
                {
                    count++;
                    if (count == 2)
                    { isStable = false; count = 0; }
                }
                basicPrimaryMp[i] = basicPrimaryMp[i - (i % 10 == 2 ? 2 : 1)] + primaryBasup;
            }

            // Race == 2
            basicSecondaryMp[0] = 60;
            basicSecondaryMp[1] = 60;
            basicSecondaryMp[2] = 78;

            int secondaryBasup = 18;
            bool boostup = false;

            for (int i = 3; i < 100; i++)
            {
                if (i % 10 == 1)
                {
                    basicSecondaryMp[i] += basicSecondaryMp[i - 1] + i + 10;
                    continue;
                }

                if (boostup)
                { secondaryBasup += 3; boostup = false; }
                else
                { secondaryBasup++; boostup = true; }

                basicSecondaryMp[i] = basicSecondaryMp[i - (i % 10 == 2 ? 2 : 1)] + secondaryBasup;
            }

            // basicXPLoad
            for (int i = 0; i < 100; i++)
            {
                basicXp[i] = i * 180;
            }

            // basicJXpLoad
            for (int i = 0; i < 100; i++)
            {
                basicJXp[i] = 360;
            }

            string fileNpcId = $"{_folder}\\monster.dat";
            string fileNpcLang = $"{_folder}\\_code_{ConfigurationManager.AppSettings["Language"]}_monster.txt";
            List<NpcMonsterDTO> npcs = new List<NpcMonsterDTO>();

            // Store like this: (vnum, (name, level))
            Dictionary<string, string> dictionaryIdLang = new Dictionary<string, string>();
            NpcMonsterDTO npc = new NpcMonsterDTO();
            List<DropDTO> drops = new List<DropDTO>();
            List<BCardDTO> monstercards = new List<BCardDTO>();
            List<NpcMonsterSkillDTO> skills = new List<NpcMonsterSkillDTO>();
            string line;
            bool itemAreaBegin = false;
            int counter = 0;
            long unknownData = 0;
            using (StreamReader npcIdLangStream = new StreamReader(fileNpcLang, CodePagesEncodingProvider.Instance.GetEncoding(1252)))
            {
                while ((line = npcIdLangStream.ReadLine()) != null)
                {
                    string[] linesave = line.Split('\t');
                    if (linesave.Length > 1 && !dictionaryIdLang.ContainsKey(linesave[0]))
                    {
                        dictionaryIdLang.Add(linesave[0], linesave[1]);
                    }
                }
                npcIdLangStream.Close();
            }
            using (StreamReader npcIdStream = new StreamReader(fileNpcId, CodePagesEncodingProvider.Instance.GetEncoding(1252)))
            {
                while ((line = npcIdStream.ReadLine()) != null)
                {
                    string[] currentLine = line.Split('\t');

                    if (currentLine.Length > 2 && currentLine[1] == "VNUM")
                    {
                        npc = new NpcMonsterDTO
                        {
                            NpcMonsterVNum = Convert.ToInt16(currentLine[2])
                        };
                        itemAreaBegin = true;
                        unknownData = 0;
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "NAME")
                    {
                        npc.Name = dictionaryIdLang.ContainsKey(currentLine[2]) ? dictionaryIdLang[currentLine[2]] : string.Empty;
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
                        npc.MaxHP = Convert.ToInt32(currentLine[2]) + basicHp[npc.Level];
                        npc.MaxMP = Convert.ToInt32(currentLine[3]) + npc.Race == 0 ? basicPrimaryMp[npc.Level] : basicSecondaryMp[npc.Level];
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "EXP")
                    {
                        npc.XP = Math.Abs(Convert.ToInt32(currentLine[2]) + basicXp[npc.Level]);
                        npc.JobXP = Convert.ToInt32(currentLine[3]) + basicJXp[npc.Level];
                        switch (npc.NpcMonsterVNum)
                        {
                            case 2500:
                                npc.HeroXp = 879;
                                break;

                            case 2501:
                                npc.HeroXp = 881;
                                break;

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

                            case 2509:
                                npc.HeroXp = 789;
                                break;

                            case 2510:
                                npc.HeroXp = 881;
                                break;

                            case 2511:
                                npc.HeroXp = 879;
                                break;

                            case 2512:
                                npc.HeroXp = 884;
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

                            
                            // percent damage monsters
                            
                            case 2309: // Foxy
                                npc.IsPercent = true;
            npc.TakeDamages = 193;
            npc.GiveDamagePercentage = 50;
            break;

                            case 2314: // renard enragé
                                npc.IsPercent = true;
            npc.TakeDamages = 3666;
            npc.GiveDamagePercentage = 10;
            break;

                            case 2315: // renard dusi enragé
                                npc.IsPercent = true;
            npc.TakeDamages = 3948;
            npc.GiveDamagePercentage = 10;
            break;

                            case 1381: // Jack o lantern
                                npc.IsPercent = true;
            npc.TakeDamages = 600;
            npc.GiveDamagePercentage = 20;
            break;

                            case 2316: // Maru
                                npc.IsPercent = true;
            npc.TakeDamages = 193;
            npc.GiveDamagePercentage = 50;
            break;

                            case 1500: // Pete o peng
                                npc.IsPercent = true;
            npc.TakeDamages = 338;
            npc.GiveDamagePercentage = 20;
            break;

                            case 774: // Reine poule
                                npc.IsPercent = true;
            npc.TakeDamages = 338;
            npc.GiveDamagePercentage = 20;
            break;

                            case 2331: // Hongbi
                                npc.IsPercent = true;
            npc.TakeDamages = 676;
            npc.GiveDamagePercentage = 30;
            break;

                            case 2332: // Cheongbi
                                npc.IsPercent = true;
            npc.TakeDamages = 507;
            npc.GiveDamagePercentage = 30;
            break;

                            case 2357: // Lola longoreil
                                npc.IsPercent = true;
            npc.TakeDamages = 193;
            npc.GiveDamagePercentage = 50;
            break;

                            case 1922: // Oeuf valak
                                npc.IsPercent = true;
            npc.TakeDamages = 9678;
            npc.MaxHP = 193560;
            npc.GiveDamagePercentage = 0;
            break;

                            case 532: // Tete de bonhomme de neige geant
                                npc.IsPercent = true;
            npc.TakeDamages = 193;
            npc.GiveDamagePercentage = 50;
            break;

                            case 531: // Bonhomme de neige
                                npc.IsPercent = true;
            npc.TakeDamages = 392;
            npc.GiveDamagePercentage = 10;
            break;

                            case 796: // Roi poulet
                                npc.IsPercent = true;
            npc.TakeDamages = 200;
            npc.GiveDamagePercentage = 20;
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
                                npc.DamageMinimum = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 4 + 32 + Convert.ToInt16(currentLine[4]) + Math.Round(Convert.ToDecimal((npc.Level - 1) / 5)));
                                npc.DamageMaximum = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 6 + 40 + Convert.ToInt16(currentLine[5]) - Math.Round(Convert.ToDecimal((npc.Level - 1) / 5)));
                                npc.Concentrate = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 5 + 27 + Convert.ToInt16(currentLine[6]));
                                npc.CriticalChance = Convert.ToByte(4 + Convert.ToInt16(currentLine[7]));
                                npc.CriticalRate = Convert.ToInt16(70 + Convert.ToInt16(currentLine[8]));
                                break;
                            case "2":
                                npc.DamageMinimum = Convert.ToInt16(Convert.ToInt16(currentLine[2]) * 6.5f + 23 + Convert.ToInt16(currentLine[4]));
                                npc.DamageMaximum = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 8 + 38 + Convert.ToInt16(currentLine[5]));
                                npc.Concentrate = Convert.ToInt16(70 + Convert.ToInt16(currentLine[6]));
                                break;
                        }
                    }
                    else if (currentLine.Length > 6 && currentLine[1] == "ARMOR")
                    {
                        npc.CloseDefence = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 2 + 18);
                        npc.DistanceDefence = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 3 + 17);
                        npc.MagicDefence = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 2 + 13);
                        npc.DefenceDodge = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 5 + 31);
                        npc.DistanceDefenceDodge = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 5 + 31);
                    }
                    else if (currentLine.Length > 7 && currentLine[1] == "ETC")
                    {
                        unknownData = Convert.ToInt64(currentLine[2]);
                        switch (unknownData)
                        {
                            case -2147481593:
                                npc.MonsterType = MonsterType.Special;
                                break;
                            case -2147483616:
                            case -2147483647:
                            case -2147483646:
                                if (npc.Race == 8 && npc.RaceType == 0)
                                {
                                    npc.NoAggresiveIcon = true;
                                }
                                else
                                {
                                    npc.NoAggresiveIcon = false;
                                }
                                break;
                        }
                        if (npc.NpcMonsterVNum >= 588 && npc.NpcMonsterVNum <= 607)
                        {
                            npc.MonsterType = MonsterType.Elite;
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
                        if (npc.VNumRequired != 0 || (unknownData != -2147481593 && unknownData != -2147481599 && unknownData != -1610610681))
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
                        for (int i = 2; i<currentLine.Length - 3; i += 3)
                        {
                            short vnum = short.Parse(currentLine[i]);
                            if (vnum == -1 || vnum == 0)
                            {
                                break;
                            }
                            if (DaoFactory.SkillDao.LoadById(vnum) == null || DaoFactory.NpcMonsterSkillDao.LoadByNpcMonster(npc.NpcMonsterVNum).Count(s => s.SkillVNum == vnum) != 0)
                            {
                                continue;
                            }
                            skills.Add(new NpcMonsterSkillDTO
                            {
                                SkillVNum = vnum,
                                Rate = Convert.ToInt16(currentLine[i + 1]),
                                NpcMonsterVNum = npc.NpcMonsterVNum
                            });
                        }
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "CARD")
                    {
                        for (int i = 0; i< 4; i++)
                        {
                            byte type = (byte)int.Parse(currentLine[5 * i + 2]);
                            if (type == 0 || type == 255)
                            {
                                continue;
                            }
                            int first = int.Parse(currentLine[5 * i + 3]);
BCardDTO itemCard = new BCardDTO
{
    NpcMonsterVNum = npc.NpcMonsterVNum,
    Type = type,
    SubType = (byte)(int.Parse(currentLine[5 * i + 5]) + 1 * 10 + 1 + (first > 0 ? 0 : 1)),
    IsLevelScaled = Convert.ToBoolean(first % 4),
    IsLevelDivided = (first % 4) == 2,
    FirstData = (short)((first > 0 ? first : -first) / 4),
    SecondData = (short)(int.Parse(currentLine[5 * i + 4]) / 4),
    ThirdData = (short)(int.Parse(currentLine[5 * i + 6]) / 4),
};
monstercards.Add(itemCard);
                        }
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "BASIC")
                    {
                        for (int i = 0; i< 4; i++)
                        {
                            byte type = (byte)int.Parse(currentLine[5 * i + 2]);
                            if (type == 0)
                            {
                                continue;
                            }
                            int first = int.Parse(currentLine[5 * i + 5]);
BCardDTO itemCard = new BCardDTO
{
    NpcMonsterVNum = npc.NpcMonsterVNum,
    Type = type,
    SubType = (byte)((int.Parse(currentLine[5 * i + 6]) + 1) * 10 + 1 + (first > 0 ? 0 : 1)),
    FirstData = (short)((first > 0 ? first : -first) / 4),
    SecondData = (short)(int.Parse(currentLine[5 * i + 4]) / 4),
    ThirdData = (short)(int.Parse(currentLine[5 * i + 3]) / 4),
    CastType = 1,
    IsLevelScaled = false,
    IsLevelDivided = false
};
monstercards.Add(itemCard);
                        }
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "ITEM")
                    {
                        if (DaoFactory.NpcMonsterDao.LoadByVNum(npc.NpcMonsterVNum) == null)
                        {
                            npcs.Add(npc);
                            counter++;
                        }
                        for (int i = 2; i<currentLine.Length - 3; i += 3)
                        {
                            short vnum = Convert.ToInt16(currentLine[i]);
                            if (vnum == -1)
                            {
                                break;
                            }
                            if (DaoFactory.DropDao.LoadByMonster(npc.NpcMonsterVNum).Count(s => s.ItemVNum == vnum) != 0)
                            {
                                continue;
                            }
                            drops.Add(new DropDTO
                            {
                                ItemVNum = vnum,
                                Amount = Convert.ToInt32(currentLine[i + 2]),
                                MonsterVNum = npc.NpcMonsterVNum,
                                DropChance = Convert.ToInt32(currentLine[i + 1])
                            });
                        }
                        itemAreaBegin = false;
                    }
                }
                DaoFactory.NpcMonsterDao.Insert(npcs);
                DaoFactory.NpcMonsterSkillDao.Insert(skills);
                DaoFactory.BCardDao.Insert(monstercards);
                Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("NPCMONSTERS_PARSED"), counter));
                npcIdStream.Close();
            }

            // Act 1
            drops.Add(new DropDTO { ItemVNum = 1002, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act1 });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 12000, MapTypeId = (short) MapTypeEnum.Act1 });
            drops.Add(new DropDTO { ItemVNum = 2015, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act1 });
            drops.Add(new DropDTO { ItemVNum = 2016, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act1 });
            drops.Add(new DropDTO { ItemVNum = 2023, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act1 });
            drops.Add(new DropDTO { ItemVNum = 2024, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act1 });
            drops.Add(new DropDTO { ItemVNum = 2028, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act1 });

            // Act2
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 7000, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 1028, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 1086, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 1237, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 1239, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 1241, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2100, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2101, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 900, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 900, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 900, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 900, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2118, Amount = 1, MonsterVNum = null, DropChance = 900, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2129, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2205, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2207, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2208, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2282, Amount = 1, MonsterVNum = null, DropChance = 2500, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2283, Amount = 1, MonsterVNum = null, DropChance = 1000, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2284, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 2296, Amount = 1, MonsterVNum = null, DropChance = 250, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.Act2 });
            drops.Add(new DropDTO { ItemVNum = 5853, Amount = 1, MonsterVNum = null, DropChance = 80, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 5854, Amount = 1, MonsterVNum = null, DropChance = 80, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 5855, Amount = 1, MonsterVNum = null, DropChance = 80, MapTypeId = (short) MapTypeEnum.Oasis });

            // Act3
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 8000, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 1086, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 1078, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 1235, Amount = 1, MonsterVNum = null, DropChance = 150, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 1237, Amount = 1, MonsterVNum = null, DropChance = 150, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 1238, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 1239, Amount = 1, MonsterVNum = null, DropChance = 150, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 1240, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 1241, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2100, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2101, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2118, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2129, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2205, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2207, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2208, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2282, Amount = 1, MonsterVNum = null, DropChance = 4000, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2283, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2284, Amount = 1, MonsterVNum = null, DropChance = 350, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2285, Amount = 1, MonsterVNum = null, DropChance = 150, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 2296, Amount = 1, MonsterVNum = null, DropChance = 150, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 5853, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 5854, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act3 });
            drops.Add(new DropDTO { ItemVNum = 5855, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act3 });

            // Act3.2 (Midgard)
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 6000, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 1086, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 1078, Amount = 1, MonsterVNum = null, DropChance = 250, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 1235, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 1237, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 1238, Amount = 1, MonsterVNum = null, DropChance = 20, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 1239, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 1240, Amount = 1, MonsterVNum = null, DropChance = 20, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 1241, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 60, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2100, Amount = 1, MonsterVNum = null, DropChance = 40, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2101, Amount = 1, MonsterVNum = null, DropChance = 60, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 40, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2118, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2129, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2205, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2207, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2208, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2282, Amount = 1, MonsterVNum = null, DropChance = 3500, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2283, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2284, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2285, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2296, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2600, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 2605, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 5857, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 5853, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 5854, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act32 });
            drops.Add(new DropDTO { ItemVNum = 5855, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act32 });


            // Act 3.4 Oasis 
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 7000, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 1086, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 1078, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 1235, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 1237, Amount = 1, MonsterVNum = null, DropChance = 150, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 1238, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 1239, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 1240, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 1241, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2100, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2101, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2118, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2129, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2205, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2207, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2208, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2282, Amount = 1, MonsterVNum = null, DropChance = 3000, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2283, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2284, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2285, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 2296, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 5853, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 5854, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 5855, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });
            drops.Add(new DropDTO { ItemVNum = 5999, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Oasis });

            // Act4
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 1000, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 1000, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 1010, Amount = 3, MonsterVNum = null, DropChance = 1500, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 2, MonsterVNum = null, DropChance = 3000, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 1241, Amount = 3, MonsterVNum = null, DropChance = 3000, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 1078, Amount = 3, MonsterVNum = null, DropChance = 1500, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 1246, Amount = 1, MonsterVNum = null, DropChance = 2500, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 1247, Amount = 1, MonsterVNum = null, DropChance = 2500, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 1248, Amount = 1, MonsterVNum = null, DropChance = 2500, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 1429, Amount = 1, MonsterVNum = null, DropChance = 2500, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 2296, Amount = 1, MonsterVNum = null, DropChance = 1000, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 2307, Amount = 1, MonsterVNum = null, DropChance = 1500, MapTypeId = (short) MapTypeEnum.Act4 });
            drops.Add(new DropDTO { ItemVNum = 2308, Amount = 1, MonsterVNum = null, DropChance = 1500, MapTypeId = (short) MapTypeEnum.Act4 });

            //Act4.2
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 1000, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 1000, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 1010, Amount = 3, MonsterVNum = null, DropChance = 1500, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 2, MonsterVNum = null, DropChance = 3000, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 1241, Amount = 3, MonsterVNum = null, DropChance = 3000, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 1078, Amount = 3, MonsterVNum = null, DropChance = 1500, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 1246, Amount = 1, MonsterVNum = null, DropChance = 2500, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 1247, Amount = 1, MonsterVNum = null, DropChance = 2500, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 1248, Amount = 1, MonsterVNum = null, DropChance = 2500, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 1429, Amount = 1, MonsterVNum = null, DropChance = 2500, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 2296, Amount = 1, MonsterVNum = null, DropChance = 1000, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 2307, Amount = 1, MonsterVNum = null, DropChance = 1500, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 2308, Amount = 1, MonsterVNum = null, DropChance = 1500, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 2445, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 2448, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 2449, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 2450, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 2451, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act42 });
            drops.Add(new DropDTO { ItemVNum = 5986, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act42 });


            // Act5
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 6000, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 1086, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 150, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 1872, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 1873, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 1874, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2129, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2207, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2282, Amount = 1, MonsterVNum = null, DropChance = 2500, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2283, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2284, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2285, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2351, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 2379, Amount = 1, MonsterVNum = null, DropChance = 1000, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 5853, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 5854, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act51 });
            drops.Add(new DropDTO { ItemVNum = 5855, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act51 });

            // Act5.2
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 5000, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 1086, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 1092, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 1093, Amount = 1, MonsterVNum = null, DropChance = 1500, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 1094, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 1500, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 2379, Amount = 1, MonsterVNum = null, DropChance = 3000, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 2380, Amount = 1, MonsterVNum = null, DropChance = 6000, MapTypeId = (short) MapTypeEnum.Act52 });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act52 });

            // Act6.1 Angel
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 1010, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 5000, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 1028, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 1078, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 1086, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 1092, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 1093, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 1094, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2129, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2282, Amount = 1, MonsterVNum = null, DropChance = 2000, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2283, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2284, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2285, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2446, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2806, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2807, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2813, Amount = 1, MonsterVNum = null, DropChance = 150, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2815, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2816, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2818, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 2819, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 5853, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 5854, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 5855, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act61A });
            drops.Add(new DropDTO { ItemVNum = 5880, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act61A });

            // Act6.1 Demon
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 1010, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 5000, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 1028, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 1078, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 1086, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 1092, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 1093, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 1094, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2129, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2282, Amount = 1, MonsterVNum = null, DropChance = 2000, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2283, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2284, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2285, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2446, Amount = 1, MonsterVNum = null, DropChance = 150, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2806, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2807, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2813, Amount = 1, MonsterVNum = null, DropChance = 150, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2815, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2816, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2818, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 2819, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 5853, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 5854, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 5855, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act61D });
            drops.Add(new DropDTO { ItemVNum = 5881, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act61D });

            // Act6.2
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1010, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act61 });
            drops.Add(new DropDTO { ItemVNum = 1010, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 6000, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1028, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1078, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1086, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1092, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1093, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1094, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1191, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1192, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1193, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 1194, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2129, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2452, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2453, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2454, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2455, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 2456, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 5853, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 5854, Amount = 1, MonsterVNum = null, DropChance = 50, MapTypeId = (short) MapTypeEnum.Act62 });
            drops.Add(new DropDTO { ItemVNum = 5855, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Act62 });

            // Comet plain
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 7000, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2100, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2101, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2205, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2207, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2208, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 2296, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.CometPlain });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.CometPlain });

            // Mine1
            drops.Add(new DropDTO { ItemVNum = 1002, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Mine1 });
            drops.Add(new DropDTO { ItemVNum = 1005, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Mine1 });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 11000, MapTypeId = (short) MapTypeEnum.Mine1 });

            // Mine2
            drops.Add(new DropDTO { ItemVNum = 1002, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Mine2 });
            drops.Add(new DropDTO { ItemVNum = 1005, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Mine2 });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 11000, MapTypeId = (short) MapTypeEnum.Mine2 });
            drops.Add(new DropDTO { ItemVNum = 1241, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Mine2 });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Mine2 });
            drops.Add(new DropDTO { ItemVNum = 2100, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Mine2 });
            drops.Add(new DropDTO { ItemVNum = 2101, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Mine2 });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.Mine2 });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Mine2 });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.Mine2 });
            drops.Add(new DropDTO { ItemVNum = 2205, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Mine2 });

            // MeadownOfMine
            drops.Add(new DropDTO { ItemVNum = 1002, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.MeadowOfMine });
            drops.Add(new DropDTO { ItemVNum = 1005, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.MeadowOfMine });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 10000, MapTypeId = (short) MapTypeEnum.MeadowOfMine });
            drops.Add(new DropDTO { ItemVNum = 2016, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.MeadowOfMine });
            drops.Add(new DropDTO { ItemVNum = 2023, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.MeadowOfMine });
            drops.Add(new DropDTO { ItemVNum = 2024, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.MeadowOfMine });
            drops.Add(new DropDTO { ItemVNum = 2028, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.MeadowOfMine });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.MeadowOfMine });
            drops.Add(new DropDTO { ItemVNum = 2118, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.MeadowOfMine });

            // SunnyPlain
            drops.Add(new DropDTO { ItemVNum = 1003, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 1006, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 8000, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 1078, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 1092, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 1093, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 1094, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2100, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2101, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2118, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2205, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2207, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2208, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 2296, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.SunnyPlain });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.SunnyPlain });

            // Fernon
            drops.Add(new DropDTO { ItemVNum = 1003, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 1006, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 9000, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 1092, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 1093, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 1094, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 2100, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 2101, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 2296, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Fernon });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.Fernon });

            // FernonF
            drops.Add(new DropDTO { ItemVNum = 1004, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 9000, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 1078, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 1092, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 1093, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 1094, Amount = 1, MonsterVNum = null, DropChance = 500, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2100, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2101, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 200, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2114, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2115, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2116, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2117, Amount = 1, MonsterVNum = null, DropChance = 700, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2205, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2206, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2207, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2208, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 2296, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.FernonF });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.FernonF });

            // Cliff
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 8000, MapTypeId = (short) MapTypeEnum.Cliff });
            drops.Add(new DropDTO { ItemVNum = 2098, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Cliff });
            drops.Add(new DropDTO { ItemVNum = 2099, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Cliff });
            drops.Add(new DropDTO { ItemVNum = 2100, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Cliff });
            drops.Add(new DropDTO { ItemVNum = 2101, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Cliff });
            drops.Add(new DropDTO { ItemVNum = 2102, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Cliff });
            drops.Add(new DropDTO { ItemVNum = 2296, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.Cliff });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 30, MapTypeId = (short) MapTypeEnum.Cliff });

            // LandOfTheDead
            drops.Add(new DropDTO { ItemVNum = 1007, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 1010, Amount = 1, MonsterVNum = null, DropChance = 800, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 1012, Amount = 1, MonsterVNum = null, DropChance = 8000, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 1015, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 1016, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 1078, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 1114, Amount = 1, MonsterVNum = null, DropChance = 400, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 1019, Amount = 1, MonsterVNum = null, DropChance = 2000, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 1020, Amount = 1, MonsterVNum = null, DropChance = 1200, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 1021, Amount = 1, MonsterVNum = null, DropChance = 600, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 1022, Amount = 1, MonsterVNum = null, DropChance = 300, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 1211, Amount = 1, MonsterVNum = null, DropChance = 250, MapTypeId = (short) MapTypeEnum.LandOfTheDead });
            drops.Add(new DropDTO { ItemVNum = 5119, Amount = 1, MonsterVNum = null, DropChance = 100, MapTypeId = (short) MapTypeEnum.LandOfTheDead });

            DaoFactory.DropDao.Insert(drops);
            */
        }

        public void ImportPackets()
        {
            string filePacket = $"{_folder}\\packet.txt";
            using (StreamReader packetTxtStream = new StreamReader(filePacket, CodePagesEncodingProvider.Instance.GetEncoding(1252)))
            {
                string line;
                while ((line = packetTxtStream.ReadLine()) != null)
                {
                    string[] linesave = line.Split(' ');
                    _packetList.Add(linesave);
                }
            }
        }

        public void ImportPortals()
        {
        }

        public void ImportRecipe()
        {
        }

        public void ImportRespawnMapType()
        {

        }

        public void ImportShopItems()
        {

        }

        public void ImportShops()
        {

        }

        public void ImportShopSkills()
        {

        }

        public void ImportSkills()
        {

        }

        public void ImportTeleporters()
        {

        }

        public void ImportScriptedInstances()
        {

        }

        internal void ImportItems()
        {
            _itemParser.Parse(_folder, _packetList);
        }

        #endregion
    }
}