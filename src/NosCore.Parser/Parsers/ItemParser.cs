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
using System.Threading.Tasks;
using NosCore.Packets.Enumerations;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
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
        private readonly string ItemCardDto = $"{Path.DirectorySeparatorChar}Item.dat";

        private readonly IDao<ItemDto, short> _itemDao;
        private readonly IDao<BCardDto, short> _bcardDao;
        private readonly ILogger _logger;

        public ItemParser(IDao<ItemDto, short> itemDao, IDao<BCardDto, short> bCardDao, ILogger logger)
        {
            _itemDao = itemDao;
            _bcardDao = bCardDao;
            _logger = logger;
        }

        public async Task ParseAsync(string folder)
        {
            var actionList = new Dictionary<string, Func<Dictionary<string, string[][]>, object?>>
            {
                {nameof(ItemDto.VNum), chunk => Convert.ToInt16(chunk["VNUM"][0][2])},
                {nameof(ItemDto.Price), chunk => Convert.ToInt64(chunk["VNUM"][0][3])},
                {nameof(ItemDto.ReputPrice), chunk => chunk["FLAG"][0][21] == "1" ? Convert.ToInt64(chunk["VNUM"][0][3]) : 0},
                {nameof(ItemDto.NameI18NKey), chunk => chunk["NAME"][0][2]},
                {nameof(ItemDto.Type), chunk => ImportType(chunk)},
                {nameof(ItemDto.ItemType), chunk => ImportItemType(chunk)},
                {nameof(ItemDto.ItemSubType), chunk => Convert.ToByte(chunk["INDEX"][0][4])},
                {nameof(ItemDto.EquipmentSlot), chunk => ImportEquipmentType(chunk)},
                {nameof(ItemDto.Morph), chunk =>  ImportEffect(chunk) == ItemEffectType.ApplySkinPartner ?Convert.ToInt16(chunk["INDEX"][0][5]) :
                    ImportEquipmentType(chunk) != EquipmentType.Amulet ? Convert.ToInt16(chunk["INDEX"][0][7]) : default},
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
                {nameof(ItemDto.BCards),  ImportBCards},
                {nameof(ItemDto.Effect),  chunk => ImportEffect(chunk)},
                {nameof(ItemDto.EffectValue),  chunk => ImportEffectValue(chunk)},
                {nameof(ItemDto.FireResistance),  chunk => ImportResistance(chunk, ElementType.Fire)},
                {nameof(ItemDto.DarkResistance),  chunk => ImportResistance(chunk, ElementType.Dark)},
                {nameof(ItemDto.LightResistance),  chunk => ImportResistance(chunk, ElementType.Light)},
                {nameof(ItemDto.WaterResistance),  chunk => ImportResistance(chunk, ElementType.Water)},
                {nameof(ItemDto.Hp), chunk => ImportHp(chunk)},
                {nameof(ItemDto.Mp), chunk => ImportMp(chunk)},
                {nameof(ItemDto.MinilandObjectPoint), chunk => ImportMinilandObjectPoint(chunk)},
                {nameof(ItemDto.Width), chunk => ImportWidth(chunk)},
                {nameof(ItemDto.Height), chunk => ImportHeight(chunk)},
                {nameof(ItemDto.DefenceDodge), chunk => ImportDefenceDodge(chunk)},
                {nameof(ItemDto.CloseDefence), chunk => ImportCloseDefence(chunk)},
                {nameof(ItemDto.DistanceDefence), chunk => ImportDistanceDefence(chunk)},
                {nameof(ItemDto.MagicDefence), chunk => ImportMagicDefence(chunk)},
                {nameof(ItemDto.BasicUpgrade), chunk => ImportBasicUpgrade(chunk)},
                {nameof(ItemDto.WaitDelay), chunk => ImportWaitDelay(chunk)},
                {nameof(ItemDto.ElementRate), chunk => ImportElementRate(chunk)},
                {nameof(ItemDto.Speed), chunk => ImportSpeed(chunk)},
                {nameof(ItemDto.SpType), chunk => ImportSpType(chunk)},
                {nameof(ItemDto.LevelJobMinimum), chunk => ImportLevelJobMinimum(chunk)},
                {nameof(ItemDto.ReputationMinimum), chunk => ImportReputationMinimum(chunk)},
                {nameof(ItemDto.ItemValidTime), chunk => ImportItemValidTime(chunk)},
                {nameof(ItemDto.Element), chunk => ImportElement(chunk)},
                {nameof(ItemDto.MaxCellonLvl), chunk => ImportMaxCellonLvl(chunk)},
                {nameof(ItemDto.MaxCellon), chunk => ImportMaxCellon(chunk)},
                {nameof(ItemDto.DistanceDefenceDodge), chunk => ImportDistanceDefenceDodge(chunk)},
                {nameof(ItemDto.MaximumAmmo), chunk => ImportMaximumAmmo(chunk)},
                {nameof(ItemDto.CriticalRate), chunk => ImportCriticalRate(chunk)},
                {nameof(ItemDto.CriticalLuckRate), chunk => ImportCriticalLuckRate(chunk)},
                {nameof(ItemDto.HitRate), chunk => ImportHitRate(chunk)},
                {nameof(ItemDto.DamageMaximum), chunk => ImportDamageMaximum(chunk)},
                {nameof(ItemDto.DamageMinimum), chunk => ImportDamageMinimum(chunk)},
            };

            var genericParser = new GenericParser<ItemDto>(folder + ItemCardDto,
                "END", 1, actionList, _logger);
            var items = (await genericParser.GetDtosAsync().ConfigureAwait(false)).GroupBy(p => p.VNum).Select(g => g.First()).ToList();
            foreach (var item in items)
            {
                HardcodeItem(item);
                if (item.ItemType != ItemType.Specialist)
                {
                    continue;
                }

                var elementdic = new Dictionary<ElementType, int> {
                    { ElementType.Neutral, 0 },
                    { ElementType.Fire, item.FireResistance },
                    { ElementType.Water, item.WaterResistance },
                    { ElementType.Light, item.LightResistance },
                    { ElementType.Dark, item.DarkResistance }
                }.OrderByDescending(s => s.Value).ToList();

                item.Element = elementdic.First().Key;
                if (elementdic.First().Value != 0 && elementdic.First().Value == elementdic.ElementAt(1).Value)
                {
                    item.SecondaryElement = elementdic.ElementAt(1).Key;
                }
            }
            SetVehicles(items, new Dictionary<byte, List<(short, short)>>
            {
                { 20, new List<(short, short)> { (9054, 2368), (1906, 2368), (9055, 2370), (1907, 2370), (9058, 2406), (1965, 2406), (9065, 2411),
                    (5008, 2411), (5238, 1817), (5226, 1817), (5240, 1819), (5228, 1819), (5234, 2522), (5236, 2524) } },
                { 21, new List<(short, short)> { (9070, 2429), (5117, 2429), (9073, 2432), (5152, 2432), (5196, 2517), (9078, 2520), (5232, 2520),
                    (5321, 2528), (9090, 2934), (5386, 2934), (9091, 2936), (5387, 2936), (9092, 2938), (5388, 2938), (9093, 2940), (5389, 2940),
                    (9094, 2942), (5390, 2942), (5391, 2944), (9115, 3679), (5997, 3679), (9079, 2522), (9080, 2524), (9081, 1817), (9082, 1819) } },
                { 22, new List<(short, short)> { (9083, 2526), (5319, 2526), (5323, 2530), (9086, 2928), (5330, 2928), (9088, 2932), (5360, 2932), (9084, 2528), (9085, 2930) } },
                { 14, new List<(short, short)> { (9087, 2930), (5332, 2930), (5914, 2513) } },
                { 16, new List<(short, short)> { (5173, 2511) } },
            });

            await _itemDao.TryInsertOrUpdateAsync(items).ConfigureAwait(false);
            await _bcardDao.TryInsertOrUpdateAsync(items.Where(s => s.BCards != null).SelectMany(s => s.BCards)).ConfigureAwait(false);

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
                    ItemVNum = Convert.ToInt16(chunk["VNUM"][0][2]),
                    Type = type,
                    SubType = (byte)((int.Parse(chunk["BUFF"][0][5 + 5 * i]) + 1) * 10 + 1),
                    IsLevelScaled = Convert.ToBoolean(first % 4),
                    IsLevelDivided = (uint)(first < 0 ? 0 : first) % 4 == 2,
                    FirstData = (short)(first > 0 ? first : -first / 4),
                    SecondData = (short)(int.Parse(chunk["BUFF"][0][4 + 5 * i]) / 4),
                    ThirdData = (short)(int.Parse(chunk["BUFF"][0][6 + 5 * i]) / 4)
                };
                list.Add(comb);
            }

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
                _ => "0"
            });
        }

        private int ImportEffectValue(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt32(ImportItemType(chunk) switch
            {
                ItemType.Fashion when ImportEquipmentType(chunk) == EquipmentType.Amulet => chunk["INDEX"][0][7],
                ItemType.Special when ImportEffect(chunk) == ItemEffectType.ApplySkinPartner => chunk["DATA"][0][5],
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
                _ => "0"
            });
        }


        private int ImportMinilandObjectPoint(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt32(ImportItemType(chunk) switch
            {
                ItemType.House => chunk["DATA"][0][2],
                ItemType.Garden => chunk["DATA"][0][2],
                ItemType.Minigame => chunk["DATA"][0][2],
                ItemType.Terrace => chunk["DATA"][0][2],
                ItemType.MinilandTheme => chunk["DATA"][0][2],
                _ => "0"
            });
        }
        private byte ImportWidth(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.House => chunk["DATA"][0][9],
                ItemType.Garden => chunk["DATA"][0][9],
                ItemType.Minigame => chunk["DATA"][0][9],
                ItemType.Terrace => chunk["DATA"][0][9],
                ItemType.MinilandTheme => chunk["DATA"][0][9],
                _ => "0"
            });
        }
        private byte ImportHeight(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.House => chunk["DATA"][0][10],
                ItemType.Garden => chunk["DATA"][0][10],
                ItemType.Minigame => chunk["DATA"][0][10],
                ItemType.Terrace => chunk["DATA"][0][10],
                ItemType.MinilandTheme => chunk["DATA"][0][10],
                _ => "0"
            });
        }
        private short ImportMp(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Food => chunk["DATA"][0][4],
                ItemType.Potion => chunk["DATA"][0][4],
                ItemType.Snack => chunk["DATA"][0][4],
                _ => "0"
            });
        }

        private short ImportDefenceDodge(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Armor => chunk["DATA"][0][6],
                ItemType.Fashion => chunk["DATA"][0][6],
                _ => "0"
            });
        }

        private short ImportDamageMinimum(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Weapon => chunk["DATA"][0][3],
                _ => "0"
            });
        }

        private short ImportDamageMaximum(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Weapon => chunk["DATA"][0][4],
                _ => "0"
            });
        }

        private short ImportHitRate(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Weapon => chunk["DATA"][0][5],
                _ => "0"
            });
        }

        private byte ImportCriticalLuckRate(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Weapon => chunk["DATA"][0][6],
                _ => "0"
            });
        }

        private byte ImportMaximumAmmo(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Weapon => 100,
                _ => 0
            });
        }

        private short ImportDistanceDefenceDodge(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Armor => chunk["DATA"][0][6],
                _ => "0"
            });
        }
        private short ImportCriticalRate(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Armor => chunk["DATA"][0][7],
                _ => "0"
            });
        }

        private long ImportItemValidTime(Dictionary<string, string[][]> chunk)
        {
            return ImportItemType(chunk) switch
            {
                ItemType.Jewelery when ImportEquipmentType(chunk) == EquipmentType.Amulet => Convert.ToInt64(chunk["DATA"][0][3]) / 10,
                ItemType.Fashion when ImportEquipmentType(chunk) == EquipmentType.CostumeHat || ImportEquipmentType(chunk) == EquipmentType.CostumeSuit => Convert.ToInt64(chunk["DATA"][0][13]) * 3600,
                _ => 0
            };
        }

        private ElementType ImportElement(Dictionary<string, string[][]> chunk)
        {
            return (ElementType)Enum.Parse(typeof(ElementType), ImportItemType(chunk) switch
            {
                ItemType.Jewelery when ImportEquipmentType(chunk) == EquipmentType.Fairy => chunk["DATA"][0][2],
                _ => "0"
            });
        }

        private byte ImportMaxCellonLvl(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Jewelery when ImportEquipmentType(chunk) != EquipmentType.Amulet && ImportEquipmentType(chunk) != EquipmentType.Fairy => chunk["DATA"][0][3],
                _ => "0"
            });
        }

        private byte ImportMaxCellon(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Jewelery when ImportEquipmentType(chunk) != EquipmentType.Amulet && ImportEquipmentType(chunk) != EquipmentType.Fairy => chunk["DATA"][0][4],
                _ => "0"
            });
        }

        private short ImportCloseDefence(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Armor => chunk["DATA"][0][3],
                ItemType.Fashion => chunk["DATA"][0][3],
                _ => "0"
            });
        }
        private short ImportDistanceDefence(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Armor => chunk["DATA"][0][4],
                ItemType.Fashion => chunk["DATA"][0][4],
                _ => "0"
            });
        }
        private short ImportMagicDefence(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Armor => chunk["DATA"][0][5],
                ItemType.Fashion => chunk["DATA"][0][5],
                _ => "0"
            });
        }

        private short ImportHp(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Food => chunk["DATA"][0][2],
                ItemType.Potion => chunk["DATA"][0][2],
                ItemType.Snack => chunk["DATA"][0][2],
                _ => "0"
            });
        }

        private short ImportWaitDelay(Dictionary<string, string[][]> chunk)
        {
            return ImportItemType(chunk) switch
            {
                ItemType.Special => 5000,
                _ => 0
            };
        }
        private short ImportElementRate(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToInt16(ImportItemType(chunk) switch
            {
                ItemType.Jewelery when ImportEquipmentType(chunk) == EquipmentType.Fairy => chunk["DATA"][0][3],
                ItemType.Specialist => chunk["DATA"][0][4],
                _ => "0"
            });
        }
        private byte ImportSpeed(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Specialist => chunk["DATA"][0][5],
                _ => "0"
            });
        }
        private byte ImportSpType(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Specialist => chunk["DATA"][0][13],
                _ => "0"
            });
        }
        private byte ImportLevelJobMinimum(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Specialist => chunk["DATA"][0][20],
                _ => "0"
            });
        }
        private byte ImportReputationMinimum(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Specialist => chunk["DATA"][0][21],
                _ => "0"
            });
        }

        private byte ImportBasicUpgrade(Dictionary<string, string[][]> chunk)
        {
            return Convert.ToByte(ImportItemType(chunk) switch
            {
                ItemType.Armor => chunk["DATA"][0][10],
                ItemType.Weapon => chunk["DATA"][0][10],
                _ => "0"
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
                _ => "0"
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

        private short ImportResistance(Dictionary<string, string[][]> chunk, ElementType element)
        {
            var equipmentType = ImportEquipmentType(chunk);
            return Convert.ToInt16(element switch
            {
                ElementType.Fire => equipmentType == EquipmentType.Sp ? chunk["DATA"][0][15] : chunk["DATA"][0][7],
                ElementType.Light => equipmentType == EquipmentType.Sp ? chunk["DATA"][0][17] : chunk["DATA"][0][9],
                ElementType.Water => equipmentType == EquipmentType.Sp ? chunk["DATA"][0][16] : chunk["DATA"][0][8],
                ElementType.Dark => equipmentType == EquipmentType.Sp ? chunk["DATA"][0][18] : chunk["DATA"][0][11],
                _ => "0"
            });
        }

        private void SetVehicles(List<ItemDto> items, Dictionary<byte, List<(short, short)>> vehicleDictionary)
        {
            foreach (var vehicle in vehicleDictionary)
            {
                foreach (var vehiclematch in vehicle.Value)
                {
                    var item = items.FirstOrDefault(s => s.VNum == vehiclematch.Item1);
                    if (item == null)
                    {
                        continue;
                    }

                    item.Speed = vehicle.Key;
                    item.WaitDelay = 3000;
                    item.Morph = vehiclematch.Item2;
                }
            }

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
                var _ when (item.Effect == ItemEffectType.DroppedSpRecharger || item.Effect == ItemEffectType.PremiumSpRecharger) && item.EffectValue == 1 => 30000,
                var _ when (item.Effect == ItemEffectType.DroppedSpRecharger || item.Effect == ItemEffectType.PremiumSpRecharger) && item.EffectValue == 2 => 70000,
                var _ when (item.Effect == ItemEffectType.DroppedSpRecharger || item.Effect == ItemEffectType.PremiumSpRecharger) && item.EffectValue == 3 => 180000,
                var _ when item.Effect == ItemEffectType.SpecialistMedal => 30_000,
                var x when x == 9031 || x == 1332 => 5108,
                var x when x == 9032 || x == 1333 => 5109,
                1334 => 5111,
                9035 => 5106,
                var x when x == 9036 || x == 1337 => 5110,
                var x when x == 9038 || x == 1339 => 5114,
                9033 => 5011,
                var x when x == 9034 || x == 1335 => 5107,
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
                var x when x == 287 || x == 4240 || x == 4194 || x == 4106 => ItemEffectType.Undefined,
                var x when x == 185 || x == 302 || x == 882 || x == 942 || x == 999 => ItemEffectType.BoxEffect,
                1245 => ItemEffectType.CraftedSpRecharger,
                var x when x == 1246 || x == 9020 || x == 1247 || x == 9021 || x == 1248 || x == 9022 || x == 1249 || x == 9023 => ItemEffectType.BuffPotions,
                var x when x == 5130 || x == 9072 => ItemEffectType.PetSpaceUpgrade,
                var x when x == 1272 || x == 1858 || x == 9047 || x == 1273 || x == 9024 || x == 1274 || x == 9025 || x == 9123 || x == 5675 => ItemEffectType.InventoryUpgrade,
                var x when x == 5795 || x == 5796 || x == 5797 => ItemEffectType.InventoryTicketUpgrade,
                var x when x == 1279 || x == 9029 || x == 1280 || x == 9030 || x == 1923 || x == 9056 => ItemEffectType.PetBasketUpgrade,
                var x when x == 1275 || x == 1886 || x == 9026 || x == 1276 || x == 9027 || x == 1277 || x == 9028 => ItemEffectType.PetBackpackUpgrade,
                var x when x == 5060 || x == 9066 => ItemEffectType.GoldNosMerchantUpgrade,
                var x when x == 5061 || x == 9067 || x == 5062 || x == 9068 => ItemEffectType.SilverNosMerchantUpgrade,
                5105 => ItemEffectType.ChangeGender,
                var x when x == 1336 || x == 1427 || x == 5115 => ItemEffectType.PointInitialisation,
                1981 => ItemEffectType.MarriageProposal,
                1982 => ItemEffectType.MarriageSeparation,
                var x when x >= 1894 && x <= 1903 => ItemEffectType.SealedTarotCard,
                var x when x >= 4046 && x <= 4055 => ItemEffectType.TarotCard,
                5119 => ItemEffectType.SpeedBooster,
                180 => ItemEffectType.AttackAmulet,
                181 => ItemEffectType.DefenseAmulet,
                _ => item.Effect,
            };

            item.EquipmentSlot = item.VNum switch
            {
                var x when x >= 4101 && x <= 4105 => EquipmentType.MainWeapon,
                _ => item.EquipmentSlot
            };

            item.ItemValidTime = item.VNum switch
            {
                var x when (x >= 4055 && x <= 4061) || (x > 4172 && x < 4176) || (x > 4045 && x < 4056) || (x == 967) || (x == 968) => 10800,
                _ => item.ItemValidTime
            };
        }
    }
}