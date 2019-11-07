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
using System.Text;
using ChickenAPI.Packets.Enumerations;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers.Generic;
using Serilog;

namespace NosCore.Parser.Parsers
{
    public class ItemParser
    {
        //  VNUM	{VNum}	{Price}
        //	NAME {Name}

        //  INDEX	0	0	0	0	0	0
        //	TYPE	0	0
        //	FLAG	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
        //	DATA	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
        //	BUFF	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0	0
        //	LINEDESC	0
        //{Desc}
        //    END
        //#========================================================
        private const string ItemCardDto = "\\Item.dat";

        private readonly IGenericDao<ItemDto> _itemDao;
        private readonly IGenericDao<BCardDto> _bcardDao;
        private readonly ILogger _logger;

        public ItemParser(IGenericDao<ItemDto> itemDao, IGenericDao<BCardDto> bCardDao, ILogger logger)
        {
            _itemDao = itemDao;
            _bcardDao = bCardDao;
            _logger = logger;
        }

        public void Parse(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object>>
            {
                {nameof(ItemDto.VNum), chunk => Convert.ToInt16(chunk["VNUM"][0][2])},
                {nameof(ItemDto.Price), chunk => Convert.ToInt32(chunk["VNUM"][0][3])},
                {nameof(ItemDto.ReputPrice), chunk => chunk["FLAG"][0][21] == "1" ? Convert.ToInt32(chunk["VNUM"][0][3]) : 0},
                {nameof(ItemDto.NameI18NKey), chunk => Convert.ToInt16(chunk["NAME"][0][2])},
                {nameof(ItemDto.Type), chunk => ImportItemType(chunk)},
                {nameof(ItemDto.ItemType), chunk => Convert.ToInt16(chunk["INDEX"][0][2])},
                {nameof(ItemDto.ItemSubType), chunk => Convert.ToByte(chunk["INDEX"][0][4])},
                {nameof(ItemDto.EquipmentSlot), chunk => ImportEquipmentType(chunk)},
                {nameof(ItemDto.EffectValue), chunk => ImportEquipmentType(chunk) == EquipmentType.Amulet ? Convert.ToInt16(chunk["INDEX"][0][7]) : default},
                {nameof(ItemDto.Morph), chunk => ImportEquipmentType(chunk) != EquipmentType.Amulet ? Convert.ToInt16(chunk["INDEX"][0][7]) : default},
                {nameof(ItemDto.Class), chunk => ImportEquipmentType(chunk) == EquipmentType.Fairy ? (byte)15 : Convert.ToByte(chunk["TYPE"][0][3])},
                {nameof(ItemDto.Flag8), chunk => chunk["FLAG"][0][24] == "1"},
                {nameof(ItemDto.Flag7), chunk => chunk["FLAG"][0][23] == "1"},
                {nameof(ItemDto.IsHeroic), chunk => chunk["FLAG"][0][22] == "1"},
                {nameof(ItemDto.Flag6), chunk => chunk["FLAG"][0][20] == "1"},
                {nameof(ItemDto.Sex), chunk =>  chunk["FLAG"][0][18] == "1" ? (byte)1 : chunk["FLAG"][0][17] == "1" ? (byte)2 : (byte)0},
                {nameof(ItemDto.IsColored), chunk => chunk["FLAG"][0][16] == "1"},
                {nameof(ItemDto.RequireBinding), chunk => chunk["FLAG"][0][15] == "1"},
                {nameof(ItemDto.Flag4), chunk => chunk["FLAG"][0][14] == "1"},
                {nameof(ItemDto.Flag3), chunk => chunk["FLAG"][0][13] == "1"},
                {nameof(ItemDto.Flag2), chunk => chunk["FLAG"][0][12] == "1"},
                {nameof(ItemDto.Flag1), chunk => chunk["FLAG"][0][11] == "1"},
                {nameof(ItemDto.Flag9), chunk => chunk["FLAG"][0][10] == "1"},
                {nameof(ItemDto.IsWarehouse), chunk => chunk["FLAG"][0][9] == "1"},
                {nameof(ItemDto.IsMinilandActionable), chunk => chunk["FLAG"][0][8] == "1"},
                {nameof(ItemDto.IsTradable), chunk => chunk["FLAG"][0][7] == "0"},
                {nameof(ItemDto.IsDroppable), chunk => chunk["FLAG"][0][6] == "0"},
                {nameof(ItemDto.IsSoldable), chunk => chunk["FLAG"][0][5] == "0"},
                {nameof(ItemDto.LevelMinimum),  chunk => ImportLevelMinimum(chunk)},
                {nameof(ItemDto.BCards),  chunk => ImportBCards(chunk)},
                {nameof(ItemDto.Effect),  chunk => ImportEffect(chunk)},
                {nameof(ItemDto.EffectValue),  chunk => ImportEffectValue(chunk)},
                {nameof(ItemDto.FireResistance),  chunk => ImportResistance(chunk, ElementType.Fire)},
                {nameof(ItemDto.DarkResistance),  chunk => ImportResistance(chunk, ElementType.Dark)},
                {nameof(ItemDto.LightResistance),  chunk => ImportResistance(chunk, ElementType.Light)},
                {nameof(ItemDto.WaterResistance),  chunk => ImportResistance(chunk, ElementType.Water)},
                {nameof(ItemDto.Hp), chunk => ImportHp(chunk)},
                {nameof(ItemDto.Mp), chunk => ImportMp(chunk)},
            };
            var genericParser = new GenericParser<ItemDto>(folder + ItemCardDto,
                "#========================================================", 1, actionList, _logger);
            var items = genericParser.GetDtos().GroupBy(p => p.VNum).Select(g => g.First()).ToList();
            foreach (var item in items)
            {
                HardcodeItem(item);
                if (item.ItemType == ItemType.Specialist)
                {
                    var elementdic = new Dictionary<ElementType, int> {
                        { ElementType.Neutral, 0 },
                        { ElementType.Fire, item.FireResistance },
                        { ElementType.Water, item.WaterResistance },
                        { ElementType.Light, item.LightResistance },
                        { ElementType.Dark, item.DarkResistance }
                    }.OrderByDescending(s => s.Value);

                    item.Element = elementdic.First().Key;
                    if (elementdic.First().Value != 0 && elementdic.First().Value == elementdic.ElementAt(1).Value)
                    {
                        item.SecondaryElement = elementdic.ElementAt(1).Key;
                    }
                }
            }
            _itemDao.InsertOrUpdate(items);
            _bcardDao.InsertOrUpdate(items.Where(s => s.BCards != null).SelectMany(s => s.BCards));

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ITEMS_PARSED), items.Count);
        }


        private List<BCardDto> ImportBCards(Dictionary<string, string[][]> chunk)
        {
            var list = new List<BCardDto>();
            for (var i = 0; i < 5; i++)
            {
                var type = (byte)int.Parse(chunk["BUFF"][0][2 + 5 * i]);
                if ((type == 0) || (type == 255))
                {
                    continue;
                }

                var first = int.Parse(chunk["BUFF"][0][3 + 5 * i]);
                var comb = new BCardDto
                {
                    SkillVNum = Convert.ToInt16(chunk["VNUM"][0][2]),
                    Type = type,
                    SubType = (byte)((int.Parse(chunk["BUFF"][i][5 + 5 * i]) + 1) * 10 + 1),
                    IsLevelScaled = Convert.ToBoolean(first % 4),
                    IsLevelDivided = (uint)(first < 0 ? 0 : first) % 4 == 2,
                    FirstData = (short)(first > 0 ? first : -first / 4),
                    SecondData = (short)(int.Parse(chunk["BUFF"][0][4 + 5 * i]) / 4),
                    ThirdData = (short)(int.Parse(chunk["BUFF"][0][6 + 5 * i]) / 4)
                };
                list.Add(comb);
            };

            return list;
        }

        private byte ImportLevelMinimum(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Fashion => chunk["DATA"][0][2],
                ItemType.Armor => chunk["DATA"][0][2],
                ItemType.Weapon => chunk["DATA"][0][2],
                ItemType.Jewelery when ImportEquipmentType(chunk) != EquipmentType.Fairy => chunk["DATA"][0][2],
                ItemType.Box => chunk["DATA"][0][4],
                _ => 0
            });
        }

        private byte ImportEffectValue(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Box => chunk["DATA"][0][3],
                ItemType.Event => chunk["DATA"][0][7],
                ItemType.Magical => chunk["DATA"][0][4],
                ItemType.Production => chunk["DATA"][0][4],
                ItemType.Map => chunk["DATA"][0][4],
                ItemType.Main => chunk["DATA"][0][4],
                ItemType.Teacher => chunk["DATA"][0][4],
                ItemType.Special => chunk["DATA"][0][4],
                ItemType.House => chunk["DATA"][0][8],
                ItemType.Garden => chunk["DATA"][0][8],
                ItemType.Minigame => chunk["DATA"][0][8],
                ItemType.Terrace => chunk["DATA"][0][8],
                ItemType.MinilandTheme => chunk["DATA"][0][8],
                _ => 0
            });
        }

        private byte ImportMp(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Food => chunk["DATA"][0][4],
                ItemType.Potion => chunk["DATA"][0][4],
                ItemType.Snack => chunk["DATA"][0][4],
                _ => 0
            });
        }

        private byte ImportHp(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Food => chunk["DATA"][0][2],
                ItemType.Potion => chunk["DATA"][0][2],
                ItemType.Snack => chunk["DATA"][0][2],
                _ => 0
            });
        }

        private ItemEffectType ImportEffect(Dictionary<string, string[][]> chunk)
        {
            return (ItemEffectType)Convert.ToUInt16(ImportItemType(chunk) switch
            {
                ItemType.Special => chunk["DATA"][0][2],
                ItemType.Box => chunk["DATA"][0][2],
                ItemType.Magical => chunk["DATA"][0][2],
                ItemType.Production => chunk["DATA"][0][2],
                ItemType.Map => chunk["DATA"][0][2],
                ItemType.Main => chunk["DATA"][0][2],
                ItemType.Teacher => chunk["DATA"][0][2],
                ItemType.Upgrade => chunk["DATA"][0][2],
                _ => ItemEffectType.NoEffect
            });
        }

        private EquipmentType ImportEquipmentType(Dictionary<string, string[][]> chunks) => (EquipmentType)Enum.Parse(typeof(EquipmentType),
                chunks["INDEX"][0][5] != "-1" ? chunks["INDEX"][0][5] : "0");

        private ItemType ImportItemType(Dictionary<string, string[][]> chunk) => chunk["INDEX"][0][3] != "-1"
                    ? (ItemType)Enum.Parse(typeof(ItemType), $"{(short)ImportType(chunk)}{chunk["INDEX"][0][3]}")
                    : ItemType.Weapon;

        private NoscorePocketType ImportType(Dictionary<string, string[][]> chunks)
        {
            return Convert.ToByte(chunks["INDEX"][0][2]) switch
            {
                4 => NoscorePocketType.Equipment,
                8 => NoscorePocketType.Equipment,
                9 => NoscorePocketType.Main,
                10 => NoscorePocketType.Etc,
                _ => (NoscorePocketType)Enum.Parse(typeof(NoscorePocketType), chunks["INDEX"][0][2])
            };
        }

        private byte ImportResistance(Dictionary<string, string[][]> chunk, ElementType element)
        {
            var equipmentType = ImportEquipmentType(chunk);
            return Convert.ToByte(element switch
            {
                ElementType.Fire => equipmentType == EquipmentType.Sp ? chunk["DATA"][0][15] : chunk["DATA"][0][7],
                ElementType.Light => equipmentType == EquipmentType.Sp ? chunk["DATA"][0][17] : chunk["DATA"][0][9],
                ElementType.Water => equipmentType == EquipmentType.Sp ? chunk["DATA"][0][16] : chunk["DATA"][0][8],
                ElementType.Dark => equipmentType == EquipmentType.Sp ? chunk["DATA"][0][18] : chunk["DATA"][0][11],
                _ => ElementType.Neutral
            });
        }

        private void HardcodeItem(ItemDto item)
        {
            if (item.EquipmentSlot == EquipmentType.Fairy)
            {
                item.MaxElementRate = item.VNum <= 256
                    ? (short)50
                    : (short)(item.ElementRate switch
                    {
                        0 => (item.VNum >= 800) && (item.VNum <= 804) ? (short)50 : (short)70,
                        30 => (item.VNum >= 884) && (item.VNum <= 887) ? (short)50 : (short)30,
                        35 => 35,
                        40 => 70,
                        50 => 80,
                        _ => 0,
                    });
            }

            item.EffectValue = item.VNum switch
            {
                var x when x == 9031 || x == 1332 => 5108,
                var x when x == 9032 || x == 1333 => 5109,
                1334 => 5111,
                9035 => 5106,
                var x when x == 9036 || x == 1337 => 5110,
                var x when x == 9038 || x == 1339 => 5114,
                9033 => 5011,
                var x when x == 9034 || x == 1335 => 5107,
                // EffectItems aka. fireworks
                1581 => 860,
                1582 => 861,
                var x when x == 1585 || x == 9044 => 859,
                var x when x == 9059 || x == 1983 => 875,
                var x when x == 9060 || x == 1984 => 876,
                var x when x == 9061 || x == 1985 => 877,
                var x when x == 9062 || x == 1986 => 878,
                var x when x == 9063 || x == 1987 => 879,
                var x when x == 1988 || x == 9064 => 880,
                4503 => 4544,
                4504 => 4294,
                var x when x == 282 || x == 283 || x == 284 || x == 4264 || x == 4262 || x == 4194 || x == 1248 || x == 9022 => 3,
                var x when x == 287 || x == 1246 || x == 9020 || x == 5062 || x == 9068 => 1,
                var x when x == 4240 || x == 1247 || x == 9021 => 2,
                var x when x == 4106 || x == 1249 || x == 9023 => 4,
                1245 => 10_000,
                var x when x == 1272 || x == 1858 || x == 9047 || x == 1923 || x == 9056 || x == 1275 || x == 1886 || x == 9026 => 10,
                var x when x == 1273 || x == 9024 || x == 5795 || x == 1279 || x == 9029 || x == 1276 || x == 9027 || x == 5060 || x == 9066 => 30,
                var x when x == 1274 || x == 9025 || x == 5796 || x == 1280 || x == 9030 || x == 1277 || x == 9028 || x == 5519 => 60,
                var x when x == 9123 || x == 5675 || x == 5797 => 0,
                var x when x == 5061 || x == 9067 => 7,
                1336 => 5106,
                var x when x >= 1894 && x <= 1903 => item.VNum + 2152,
                // UpgradeItems (needed to be hardcoded)
                1218 => 26,
                1363 => 27,
                1364 => 28,
                5107 => 47,
                5207 => 50,
                5369 => 61,
                _ => item.EffectValue
            };

            item.Element = item.VNum switch
            {
                901 => ElementType.Fire,
                903 => ElementType.Water,
                906 => ElementType.Light,
                909 => ElementType.Light,
                _ => item.Element,
            };

            item.Effect = item.VNum switch
            {
                var x when item.ItemType == ItemType.Special && ((x > 5891 && x < 5900) || (x > 9100 && x < 9109)) => ItemEffectType.Undefined,
                var x when x > 2059 && x < 2070 => ItemEffectType.ApplyHairDie,
                282 => ItemEffectType.RedAmulet,
                283 => ItemEffectType.BlueAmulet,
                284 => ItemEffectType.ReinforcementAmulet,
                4264 => ItemEffectType.Heroic,
                4262 => ItemEffectType.RandomHeroic,
                var x when x == 287 || x== 4240 || x == 4194 || x == 4106 => ItemEffectType.Undefined,
                var x when x == 185 || x == 302 || x == 882 || x == 942 || x == 999 => ItemEffectType.BoxEffect,
                1245 => ItemEffectType.CraftedSpRecharger,
                var x when x == 1246 || x == 9020 || x == 1247 || x == 9021 || x == 1248 || x == 9022 || x == 1249 || x == 9023 => ItemEffectType.BuffPotions,
                var x when x == 5130 || x == 9072 => ItemEffectType.PetSpaceUpgrade,
                var x when x == 1272 || x == 1858 || x == 9047 || x == 1273 || x == 9024 || x == 1274 || x == 9025 || x == 9123 || x == 5675 => ItemEffectType.InventoryUpgrade,
                var x when x == 5795 || x == 5796 || x == 5797 => ItemEffectType.InventoryTicketUpgrade,
                var x when x == 1279 || x == 9029 || x == 1280 || x == 9030 || x == 1923 || x == 9056  => ItemEffectType.PetBasketUpgrade,
                var x when x == 1275 || x == 1886 || x == 9026 || x == 1276 || x == 9027 || x == 1277 || x == 9028 => ItemEffectType.PetBackpackUpgrade,
                var x when x == 5060 || x == 9066 => ItemEffectType.GoldNosMerchantUpgrade,
                var x when x == 5061 || x == 9067 || x == 5062 || x == 9068 => ItemEffectType.SilverNosMerchantUpgrade,
                5105 => ItemEffectType.ChangeGender,
                var x when x == 1336 || x == 1427 || x == 5115 => ItemEffectType.PointInitialisation,
                1981 => ItemEffectType.MarriageProposal, // imagined number as for I = √(-1), complex z = a + bi
                1982 => ItemEffectType.MarriageSeparation, // imagined number as for I = √(-1), complex z = a + bi
                var x when x >= 1894 && x <= 1903 => ItemEffectType.SealedTarotCard,
                var x when x >= 4046 && x <= 4055 => ItemEffectType.TarotCard,
                5119 => ItemEffectType.SpeedBooster,
                180 => ItemEffectType.AttackAmulet,
                181 => ItemEffectType.DefenseAmulet,
                _ => item.Effect,
            };

            //               ItemType.Special:
            //                switch (item.Effect)
            //                {
            //                    case ItemEffectType.DroppedSpRecharger:
            //                    case ItemEffectType.PremiumSpRecharger:
            //                        if (Convert.ToInt32(currentLine[4]) == 1)
            //                        {
            //                            item.EffectValue = 30000;
            //                        }
            //                        else if (Convert.ToInt32(currentLine[4]) == 2)
            //                        {
            //                            item.EffectValue = 70000;
            //                        }
            //                        else if (Convert.ToInt32(currentLine[4]) == 3)
            //                        {
            //                            item.EffectValue = 180000;
            //                        }
            //                        break;

            //                    case ItemEffectType.SpecialistMedal:
            //                        item.EffectValue = 30_000;
            //                        break;

            //                    case ItemEffectType.ApplySkinPartner:
            //                        item.EffectValue = Convert.ToInt32(currentLine[5]);
            //                        item.Morph = Convert.ToInt16(currentLine[4]);
            //                        break;
            //                }
            //                    case EquipmentType.Amulet:
            //                        item.ItemValidTime = ((item.VNum > 4055) && (item.VNum < 4061))
            //                            || ((item.VNum > 4172) && (item.VNum < 4176)) ||
            //                            ((item.VNum > 4045) && (item.VNum < 4056)) || (item.VNum == 967) ||
            //                            (item.VNum == 968) ? 10800 : Convert.ToInt32(currentLine[3]) / 10;
            //                        break;

            //    case 4101:
            //    case 4102:
            //    case 4103:
            //    case 4104:
            //    case 4105:
            //        item.EquipmentSlot = 0;
            //break;

            //    case 9054:
            //    case 1906:
            //        item.Morph = 2368;
            //item.Speed = 20;
            //item.WaitDelay = 3000;
            //break;

            //    case 9055:
            //    case 1907:
            //        item.Morph = 2370;
            //item.Speed = 20;
            //item.WaitDelay = 3000;
            //break;

            //    case 9058:
            //    case 1965:
            //        item.Morph = 2406;
            //item.Speed = 20;
            //item.WaitDelay = 3000;
            //break;

            //    case 9065:
            //    case 5008:
            //        item.Morph = 2411;
            //item.Speed = 20;
            //item.WaitDelay = 3000;
            //break;

            //    case 9070:
            //    case 5117:
            //        item.Morph = 2429;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 9073:
            //    case 5152:
            //        item.Morph = 2432;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 5173:
            //        item.Morph = 2511;
            //item.Speed = 16;
            //item.WaitDelay = 3000;
            //break;

            //    case 5196:
            //        item.Morph = 2517;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 5238:
            //    case 5226: // Invisible locomotion, only 5 seconds with booster
            //        item.Morph = 1817;
            //item.Speed = 20;
            //item.WaitDelay = 3000;
            //break;
            //    case 5240:
            //    case 5228: // Invisible locoomotion, only 5 seconds with booster
            //        item.Morph = 1819;
            //item.Speed = 20;
            //item.WaitDelay = 3000;
            //break;

            //    case 9078:
            //    case 5232:
            //        item.Morph = 2520;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 5234:
            //        item.Morph = 2522;
            //item.Speed = 20;
            //item.WaitDelay = 3000;
            //break;

            //    case 5236:
            //        item.Morph = 2524;
            //item.Speed = 20;
            //item.WaitDelay = 3000;
            //break;

            //    case 9083:
            //    case 5319:
            //        item.Morph = 2526;
            //item.Speed = 22;
            //item.WaitDelay = 3000;
            //break;

            //    case 5321:
            //        item.Morph = 2528;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 5323:
            //        item.Morph = 2530;
            //item.Speed = 22;
            //item.WaitDelay = 3000;
            //break;

            //    case 9086:
            //    case 5330:
            //        item.Morph = 2928;
            //item.Speed = 22;
            //item.WaitDelay = 3000;
            //break;

            //    case 9087:
            //    case 5332:
            //        item.Morph = 2930;
            //item.Speed = 14;
            //item.WaitDelay = 3000;
            //break;

            //    case 9088:
            //    case 5360:
            //        item.Morph = 2932;
            //item.Speed = 22;
            //item.WaitDelay = 3000;
            //break;

            //    case 9090:
            //    case 5386:
            //        item.Morph = 2934;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 9091:
            //    case 5387:
            //        item.Morph = 2936;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 9092:
            //    case 5388:
            //        item.Morph = 2938;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 9093:
            //    case 5389:
            //        item.Morph = 2940;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 9094:
            //    case 5390:
            //        item.Morph = 2942;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 5391:
            //        item.Morph = 2944;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 5914:
            //        item.Morph = 2513;
            //item.Speed = 14;
            //item.WaitDelay = 3000;
            //break;

            //    case 9115:
            //    case 5997:
            //        item.Morph = 3679;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 9079:
            //        item.Morph = 2522;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 9080:
            //        item.Morph = 2524;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 9081:
            //        item.Morph = 1817;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 9082:
            //        item.Morph = 1819;
            //item.Speed = 21;
            //item.WaitDelay = 3000;
            //break;

            //    case 9084:
            //        item.Morph = 2528;
            //item.Speed = 22;
            //item.WaitDelay = 3000;
            //break;

            //    case 9085:
            //        item.Morph = 2930;
            //item.Speed = 22;
            //item.WaitDelay = 3000;
            //break;

        }



        //public void Parse(string folder)
        //    else if ((currentLine.Length > 1) && (currentLine[1] == "DATA"))
        //    {
        //case ItemType.Weapon:
        //  case ItemType.Armor:
        //     item.BasicUpgrade = Convert.ToByte(currentLine[10]);

        // case ItemType.Armor:
        //case ItemType.Fashion:
        //                item.DefenceDodge = Convert.ToInt16(currentLine[6]);
        //                item.CloseDefence = Convert.ToInt16(currentLine[3]);
        //                item.DistanceDefence = Convert.ToInt16(currentLine[4]);
        //                item.MagicDefence = Convert.ToInt16(currentLine[5]);

        //          case ItemType.Weapon:
        //                item.DamageMinimum = Convert.ToInt16(currentLine[3]);
        //                item.DamageMaximum = Convert.ToInt16(currentLine[4]);
        //                item.HitRate = Convert.ToInt16(currentLine[5]);
        //                item.CriticalLuckRate = Convert.ToByte(currentLine[6]);
        //                item.CriticalRate = Convert.ToInt16(currentLine[7]);
        //                item.MaximumAmmo = 100;
        //                break;

        //            case ItemType.Armor:
        //                item.DistanceDefenceDodge = Convert.ToInt16(currentLine[6]);
        //                break;

        //            case ItemType.Fashion:
        //                if (item.EquipmentSlot.Equals(EquipmentType.CostumeHat)
        //                    || item.EquipmentSlot.Equals(EquipmentType.CostumeSuit))
        //                {
        //                    item.ItemValidTime = Convert.ToInt32(currentLine[13]) * 3600;
        //                }

        //                break;

        //            case ItemType.Jewelery:
        //                switch (item.EquipmentSlot)
        //                {
        //                    case EquipmentType.Amulet:
        //                        item.ItemValidTime = Convert.ToInt32(currentLine[3]) / 10;
        //                        break;
        //                    case EquipmentType.Fairy:
        //                        item.Element = (ElementType)Enum.Parse(typeof(ElementType), currentLine[2]);
        //                        item.ElementRate = Convert.ToInt16(currentLine[3]);
        //                        break;
        //                    default:
        //                        item.MaxCellonLvl = Convert.ToByte(currentLine[3]);
        //                        item.MaxCellon = Convert.ToByte(currentLine[4]);
        //                        break;
        //                }

        //                break;
        //            case ItemType.Special:
        //                item.WaitDelay = 5000;
        //                break;

        //            case ItemType.Specialist:
        //                item.ElementRate = Convert.ToInt16(currentLine[4]);
        //                item.Speed = Convert.ToByte(currentLine[5]);
        //                item.SpType = Convert.ToByte(currentLine[13]);
        //                item.LevelJobMinimum = Convert.ToByte(currentLine[20]);
        //                item.ReputationMinimum = Convert.ToByte(currentLine[21]);
        //                break;

        //           case ItemType.House:
        //           case ItemType.Garden:
        //           case ItemType.Minigame :
        //           case ItemType.Terrace:
        //           case ItemType.MinilandTheme:
        //                item.MinilandObjectPoint = int.Parse(currentLine[2]);
        //                item.Width = Convert.ToByte(currentLine[9]);
        //                item.Height = Convert.ToByte(currentLine[10]);
        //          break;

        //    }


    }
}