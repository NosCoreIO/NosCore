//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Parser.Parsers.Generic;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Parsers
{
    public class ItemParser(IDao<ItemDto, short> itemDao, IDao<BCardDto, int> bCardDao, ILoggerFactory loggerFactory, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        private readonly ILogger<ItemParser> _logger = loggerFactory.CreateLogger<ItemParser>();
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
        private readonly string _itemCardDto = $"{Path.DirectorySeparatorChar}Item.dat";

        public FluentParserBuilder<ItemDto> BuildParser(string folder)
        {
            return FluentParserBuilder<ItemDto>.Create(folder + _itemCardDto, "END", 1)
                .Field(x => x.VNum, "VNUM", 0, 2, s => Convert.ToInt16(s), "Item vnum")
                .Field(x => x.Price, "VNUM", 0, 3, s => Convert.ToInt64(s), "NPC shop sell price")
                .Field(x => x.ReputPrice, chunk => chunk["FLAG"][0][21] == "1" ? Convert.ToInt64(chunk["VNUM"][0][3]) : 0,
                    source: "FLAG[20] + VNUM[1]", description: "Reputation price when FLAG[20] is set")
                .Field(x => x.NameI18NKey, "NAME", 0, 2, s => s, "Localization key (zts##e)")
                .Field(x => x.Type, chunk => ImportType(chunk),
                    source: "FLAG[26] + DATA[0] + INDEX[0,1]",
                    description: "Pocket type: FLAG[26]==1 -> Raid, DATA[0]==1000 && INDEX[1]==4 -> Mount, else legacy INDEX[0] map")
                .Field(x => x.ItemType, chunk => ImportItemType(chunk),
                    source: "INDEX[0,1]", description: "ItemType composite enum, built from the INDEX pocket even for Raid and Mount items")
                .Field(x => x.ItemSubType, "INDEX", 0, 4, s => Convert.ToByte(s), "Subtype within item category")
                .Field(x => x.EquipmentSlot, chunk => ImportEquipmentType(chunk),
                    source: "INDEX[3]", description: "Equipment slot")
                .Field(x => x.Morph, chunk => ImportEffect(chunk) == ItemEffectType.ApplySkinPartner ? Convert.ToInt16(chunk["DATA"][0][4]) :
                    ImportEquipmentType(chunk) != EquipmentType.Amulet ? Convert.ToInt16(chunk["INDEX"][0][7]) : default,
                    source: "DATA[4] (partner skin) or INDEX[7]", description: "Morph id: the partner skin's look, else the item's own")
                .Field(x => x.Class, chunk => ImportEquipmentType(chunk) == EquipmentType.Fairy ? (byte)15 : Convert.ToByte(chunk["TYPE"][0][3]),
                    source: "TYPE[1] (fairy override: 15)", description: "Class restriction bitmask")
                .Field(x => x.Flag8, "FLAG", 0, 24, s => s == "1", "FLAG bit 23 (unknown)")
                .Field(x => x.Flag7, "FLAG", 0, 23, s => s == "1", "Headgear that also hides the mask; only changes what the client draws")
                .Field(x => x.IsHeroic, "FLAG", 0, 22, s => s == "1", "Heroic item")
                .Field(x => x.Flag6, "FLAG", 0, 20, s => s == "1", "Set on 17 upgrade materials (soul gems, dragon parts, completion stones, guardian angels); the client never reads it")
                .Field(x => x.Sex, chunk => chunk["FLAG"][0][18] == "1" ? (byte)1 : chunk["FLAG"][0][17] == "1" ? (byte)2 : (byte)0,
                    source: "FLAG[16] + FLAG[17]", description: "Genders allowed, one bit per GenderType: 1=male only, 2=female only, 0=any")
                .Field(x => x.IsColored, "FLAG", 0, 16, s => s == "1", "Uses design slot for color")
                .Field(x => x.RequireBinding, "FLAG", 0, 15, s => s == "1", "Binds to character on use/equip")
                .Field(x => x.Flag4, "FLAG", 0, 14, s => s == "1", "Used on another item: double-clicking it picks a target item instead of sending u_i")
                .Field(x => x.Flag3, "FLAG", 0, 13, s => s == "1", "On a miniland object the tooltip shows \"In use\" or \"Not used\"; set on 1418 items, most of them mall goods")
                .Field(x => x.Flag2, "FLAG", 0, 12, s => s == "1", "FLAG bit 11 (set on mounts + raid items + partner equipment)")
                .Field(x => x.Flag1, "FLAG", 0, 11, s => s == "1", "FLAG bit 10 (unknown)")
                .Field(x => x.Flag9, "FLAG", 0, 10, s => s == "1", "FLAG bit 9 (appears on beads, medals, raid seals)")
                .Field(x => x.IsWarehouse, "FLAG", 0, 9, s => s == "1", "Can be stored in the account warehouse")
                .Field(x => x.IsMinilandActionable, "FLAG", 0, 8, s => s == "1", "Miniland-actionable object")
                .Field(x => x.IsTradable, "FLAG", 0, 7, s => s == "0", "Inverted: dat=0 -> tradable")
                .Field(x => x.IsDroppable, "FLAG", 0, 6, s => s == "0", "Inverted: dat=0 -> droppable")
                .Field(x => x.IsSoldable, "FLAG", 0, 5, s => s == "0", "Inverted: dat=0 -> soldable")
                .Field(x => x.LevelMinimum, chunk => ImportLevelMinimum(chunk),
                    source: "DATA[varies]", description: "Minimum level to equip/use, per-ItemType")
                .Field(x => x.BCards, chunk => ImportBCards(chunk),
                    source: "BUFF[0..24]", description: "Up to 5 BCard groups (5 fields each)")
                .Field(x => x.Effect, chunk => ImportEffect(chunk),
                    source: "DATA[varies] per ItemType", description: "ItemEffectType enum")
                .Field(x => x.EffectValue, chunk => ImportEffectValue(chunk),
                    source: "DATA[varies] per ItemType", description: "Effect numeric argument")
                .Field(x => x.FireResistance, chunk => ImportResistance(chunk, ElementType.Fire),
                    source: "DATA[7] or DATA[15]", description: "Fire resistance; SP uses DATA[15], others DATA[7]")
                .Field(x => x.DarkResistance, chunk => ImportResistance(chunk, ElementType.Dark),
                    source: "DATA[11] or DATA[18]", description: "Dark resistance; SP uses DATA[18]")
                .Field(x => x.LightResistance, chunk => ImportResistance(chunk, ElementType.Light),
                    source: "DATA[9] or DATA[17]", description: "Light resistance; SP uses DATA[17]")
                .Field(x => x.WaterResistance, chunk => ImportResistance(chunk, ElementType.Water),
                    source: "DATA[8] or DATA[16]", description: "Water resistance; SP uses DATA[16]")
                .Field(x => x.Hp, chunk => ImportHp(chunk), source: "DATA[varies]", description: "Hp bonus on equip")
                .Field(x => x.Mp, chunk => ImportMp(chunk), source: "DATA[varies]", description: "Mp bonus on equip")
                .Field(x => x.MinilandObjectPoint, chunk => ImportMinilandObjectPoint(chunk),
                    source: "DATA[2] (miniland only)", description: "Miniland point value")
                .Field(x => x.Width, chunk => ImportWidth(chunk),
                    source: "DATA[3] (miniland only)", description: "Miniland object footprint width")
                .Field(x => x.Height, chunk => ImportHeight(chunk),
                    source: "DATA[4] (miniland only)", description: "Miniland object footprint height")
                .Field(x => x.DefenceDodge, chunk => ImportDefenceDodge(chunk),
                    source: "DATA[varies]", description: "Dodge chance")
                .Field(x => x.CloseDefence, chunk => ImportCloseDefence(chunk),
                    source: "DATA[varies]", description: "Melee defence")
                .Field(x => x.DistanceDefence, chunk => ImportDistanceDefence(chunk),
                    source: "DATA[varies]", description: "Ranged defence")
                .Field(x => x.MagicDefence, chunk => ImportMagicDefence(chunk),
                    source: "DATA[varies]", description: "Magic defence")
                .Field(x => x.BasicUpgrade, chunk => ImportBasicUpgrade(chunk),
                    source: "DATA[varies]", description: "Base upgrade level")
                .Field(x => x.WaitDelay, chunk => ImportWaitDelay(chunk),
                    source: "DATA[varies]", description: "Cooldown / use delay")
                .Field(x => x.ElementRate, chunk => ImportElementRate(chunk),
                    source: "DATA[varies]", description: "Element rate %")
                .Field(x => x.Speed, chunk => ImportSpeed(chunk),
                    source: "DATA[varies]", description: "Movement speed bonus / vehicle speed")
                .Field(x => x.SpType, chunk => ImportSpType(chunk),
                    source: "DATA[varies] (SP only)", description: "SP transformation type")
                .Field(x => x.PartnerSkillSet, chunk => ImportPartnerSkillSet(chunk),
                    source: "DATA[14] (partner SP only)", description: "Partner SP skill set: the UpgradeType shared by its three partner skills, one more than the raw value")
                .Field(x => x.LevelJobMinimum, chunk => ImportLevelJobMinimum(chunk),
                    source: "DATA[varies]", description: "Minimum job level to use/equip")
                .Field(x => x.ReputationMinimum, chunk => ImportReputationMinimum(chunk),
                    source: "DATA[varies]", description: "Minimum reputation tier")
                .Field(x => x.ItemValidTime, chunk => ImportItemValidTime(chunk),
                    source: "DATA[varies]", description: "Expiration duration in seconds")
                .Field(x => x.Element, chunk => ImportElement(chunk),
                    source: "DATA[varies]", description: "Primary elemental alignment")
                .Field(x => x.MaxCellonLvl, chunk => ImportMaxCellonLvl(chunk),
                    source: "DATA[varies]", description: "Max cellon level")
                .Field(x => x.MaxCellon, chunk => ImportMaxCellon(chunk),
                    source: "DATA[varies]", description: "Max cellons slotted")
                .Field(x => x.ShellType, chunk => ImportShellType(chunk),
                    source: "DATA[5] (shell only)", description: "Shell kind, which decides the options it rolls")
                .Field(x => x.DistanceDefenceDodge, chunk => ImportDistanceDefenceDodge(chunk),
                    source: "DATA[varies]", description: "Ranged dodge chance")
                .Field(x => x.MaximumAmmo, chunk => ImportMaximumAmmo(chunk),
                    source: "DATA[varies]", description: "Max ammo")
                .Field(x => x.CriticalRate, chunk => ImportCriticalRate(chunk),
                    source: "DATA[varies]", description: "Critical hit chance")
                .Field(x => x.CriticalLuckRate, chunk => ImportCriticalLuckRate(chunk),
                    source: "DATA[varies]", description: "Critical luck multiplier")
                .Field(x => x.HitRate, chunk => ImportHitRate(chunk),
                    source: "DATA[varies]", description: "Hit rate")
                .Field(x => x.DamageMaximum, chunk => ImportDamageMaximum(chunk),
                    source: "DATA[varies]", description: "Max weapon damage")
                .Field(x => x.DamageMinimum, chunk => ImportDamageMinimum(chunk),
                    source: "DATA[varies]", description: "Min weapon damage")
                .Describe("FLAG", "25 boolean bits. The last one identifies raid-inventory items (seals, boxes, chests, drops).")
                .Doc("FLAG", 17, "FemaleOnly", "Set together with FLAG[18]=0 to mark a female-only item; rolled up into Sex.")
                .Doc("FLAG", 18, "MaleOnly", "Set together with FLAG[17]=0 to mark a male-only item; rolled up into Sex.")
                .Doc("FLAG", 2, "Unused", "0 on every item; the client reads it as a number, not a flag.")
                .Doc("FLAG", 3, "Unused", "0 on every item.")
                .Doc("FLAG", 4, "NotAvailable", "The shop tooltip adds \"Not available\"; set on 3 items only (333, 334, 1225).")
                .Doc("FLAG", 19, "Unused", "0 on every item.")
                .Doc("FLAG", 21, "ReputationPrice", "The shop tooltip labels the price \"Reputation\"; read into ReputPrice.")
                .Doc("FLAG", 25, "Unused", "0 on every item.")
                .Doc("FLAG", 26, "RaidItem", "Set on raid seals, raid boxes, chests and drops; routes the item to the Raid pocket.")
                .Doc("INDEX", 2, "Pocket", "The pocket the item goes to (8, 9, 10 are Equipment, Main, Etc); read into Type.")
                .Doc("INDEX", 3, "Category", "The kind of item within its pocket; read with the pocket into ItemType.");
        }

        public async Task ParseAsync(string folder)
        {
            var parser = BuildParser(folder).Build(loggerFactory, logLanguage);
            var items = (await parser.GetDtosAsync()).GroupBy(p => p.VNum).Select(g => g.First()).ToList();
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
            foreach (var item in items)
            {
                if (Mounts.All.TryGetValue(item.VNum, out var mount))
                {
                    item.Speed = mount.Speed;
                    item.WaitDelay = 3000;
                    item.Morph = mount.Morph;
                }
            }

            await itemDao.TryInsertOrUpdateAsync(items);
            var itemVNums = items.Select(s => (short?)s.VNum).ToHashSet();
            await bCardDao.TryDeleteAsync(bCardDao.Where(s => itemVNums.Contains(s.ItemVNum))?.Select(s => s.BCardId).ToList() ?? []);
            await bCardDao.TryInsertOrUpdateAsync(items.Where(s => s.BCards != null).SelectMany(s => s.BCards));

            _logger.LogInformation(logLanguage[LogLanguageKey.ITEMS_PARSED], items.Count);
        }


        // A negative first value selects the second half of the subtype pair, as in SkillParser
        // and CardParser.
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
                var value = EffectValue.Decode(first);
                var comb = new BCardDto
                {
                    ItemVNum = Convert.ToInt16(chunk["VNUM"][0][2]),
                    Type = type,
                    SubType = (byte)(((int.Parse(chunk["BUFF"][0][5 + 5 * i]) + 1) * 10) + 1
                        + (first < 0 ? 1 : 0)),
                    IsLevelScaled = value.IsLevelScaled,
                    IsLevelDivided = value.IsLevelDivided,
                    FirstData = (short)value.Value,
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
                ItemType.Magical when ImportEffect(chunk) == ItemEffectType.PassiveSkillBook => chunk["DATA"][0][5],
                // Minutes the bubble stays up; the item's description gives 30 and its data row has no duration.
                ItemType.Magical when ImportEffect(chunk) == ItemEffectType.SpeechBubble => "30",
                // How many wig colours a dye bomb picks from; its data row has no count.
                ItemType.Magical when ImportEffect(chunk) == ItemEffectType.WigDye => "15",
                ItemType.Magical => chunk["DATA"][0][4],
                ItemType.Production => chunk["DATA"][0][4],
                // Cellons carry the option tier here; every other upgrade material leaves it 0.
                ItemType.Upgrade => chunk["DATA"][0][4],
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

        private ShellType? ImportShellType(Dictionary<string, string[][]> chunk)
        {
            return ImportItemType(chunk) == ItemType.Shell ? (ShellType)Convert.ToByte(chunk["DATA"][0][5]) : null;
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
        private byte ImportPartnerSkillSet(Dictionary<string, string[][]> chunk)
        {
            return ImportItemType(chunk) == ItemType.Specialist && chunk["INDEX"][0][4] == "4"
                ? (byte)(Convert.ToByte(chunk["DATA"][0][14]) + 1)
                : (byte)0;
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
                    ? (ItemType)Enum.Parse(typeof(ItemType), $"{(short)ImportIndexPocket(chunk)}{chunk["INDEX"][0][3]}")
                    : ItemType.Weapon;

        private NoscorePocketType ImportType(Dictionary<string, string[][]> chunks)
        {
            var flag = chunks["FLAG"][0];
            if (flag.Length > 26 && flag[26] == "1") return NoscorePocketType.Raid;
            if (chunks["DATA"][0][2] == "1000" && chunks["INDEX"][0][3] == "4")
            {
                return NoscorePocketType.Mount;
            }

            return ImportIndexPocket(chunks);
        }

        private static NoscorePocketType ImportIndexPocket(Dictionary<string, string[][]> chunks)
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
                var x when x >= 392 && x <= 396 => x - 392,
                var x when x == 5838 || x == 9118 => 10,
                var x when x == 5839 || x == 9119 => 30,
                var x when x == 9031 || x == 1332 => 5108,
                var x when x == 9032 || x == 1333 => 5109,
                1334 => 5111,
                9035 => 5106,
                var x when x == 9036 || x == 1337 => 5110,
                var x when x == 9038 || x == 1339 => 5114,
                9033 => 5111,
                var x when x == 9034 || x == 1335 => 5107,
                var x when x == 5119 || x == 9071 => 885,
                var x when x == 5916 || x == 5927 => 7200,
                var x when x == 5928 || x == 5929 => 600,
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
                var x when x == 1365 || x == 9039 => 10_000,
                var x when x == 1272 || x == 1858 || x == 9047 || x == 1923 || x == 9056 || x == 1275 || x == 1886 || x == 9026 => 10,
                var x when x == 1273 || x == 9024 || x == 5795 || x == 1279 || x == 9029 || x == 1276 || x == 9027 || x == 5060 || x == 9066 => 30,
                var x when x == 1274 || x == 9025 || x == 5796 || x == 1280 || x == 9030 || x == 1277 || x == 9028 => 60,
                var x when x == 9123 || x == 5675 || x == 5797 => 0,
                var x when x == 5061 || x == 9067 => 7,
                1336 => 5106,
                var x when x >= 1894 && x <= 1903 => item.VNum + 2152,
                1218 => 26,
                1363 => 27,
                1364 => 28,
                var x when x == 5107 || x == 907 => (int)UpgradePacketType.UpgradeSpChiken,
                var x when x == 5207 || x == 900 => (int)UpgradePacketType.UpgradeSpPyjama,
                var x when x == 5519 || x == 4099 || x == 4100 => (int)UpgradePacketType.UpgradeSpPirate,
                // The production coupon's description gives its 500 points. The durability coupon's
                // 300 matches the client's refusal below 300 missing points, not a stated amount.
                1269 => 300,
                1271 => 500,
                1904 => 5,
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
                var x when item.ItemType == ItemType.Special && ((x > 5891 && x < 5900) || (x > 9100 && x < 9109)) => ItemEffectType.MedalOfHonour,
                var x when x > 2059 && x < 2070 => ItemEffectType.ApplyHairDie,
                var x when x >= 392 && x <= 396 => ItemEffectType.MinigameChest,
                282 => ItemEffectType.RedAmulet,
                283 => ItemEffectType.BlueAmulet,
                284 => ItemEffectType.ReinforcementAmulet,
                4264 => ItemEffectType.Heroic,
                4262 => ItemEffectType.RandomHeroic,
                var x when x == 287 || x == 4240 || x == 4194 || x == 4106 => ItemEffectType.Undefined,
                var x when item.ItemType == ItemType.Box && item.ItemSubType == 3 => ItemEffectType.BoxEffect,
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
                var x when x == 1366 || x == 1427 || x == 5115 || x == 9040 => ItemEffectType.PointInitialisation,
                1981 => ItemEffectType.MarriageProposal,
                1982 => ItemEffectType.MarriageSeparation,
                var x when x >= 1894 && x <= 1903 => ItemEffectType.SealedTarotCard,
                1904 => ItemEffectType.TarotCardGame,
                var x when x >= 4046 && x <= 4055 => ItemEffectType.TarotCard,
                var x when x == 5119 || x == 9071 => ItemEffectType.SpeedBooster,
                var x when x == 2160 || x == 2312 || x == 10048 => ItemEffectType.FriendshipWings,
                180 => ItemEffectType.AttackAmulet,
                181 => ItemEffectType.DefenseAmulet,
                var x when x == 5838 || x == 5839 || x == 9118 || x == 9119 => ItemEffectType.CuarryBankVipMedal,
                5836 => ItemEffectType.CuarryBankSavingsBook,
                1429 => ItemEffectType.ShellIdentification,
                1430 => ItemEffectType.ShellOptionRemoval,
                1428 => ItemEffectType.Perfume,
                var x when x == 1365 || x == 9039 => ItemEffectType.SoulRevival,
                var x when x == 5107 || x == 5207 || x == 5519 => ItemEffectType.EventSpUpgradeScroll,
                var x when x == 907 || x == 900 || x == 4099 || x == 4100 => ItemEffectType.EventSpecialist,
                var x when x == 5874 || x == 9112 => ItemEffectType.CostumeFusion,
                1269 => ItemEffectType.MinigameDurabilityCoupon,
                1271 => ItemEffectType.ProductionCoupon,
                var x when x == 5916 || x == 5927 || x == 5928 || x == 5929 => ItemEffectType.HeatResistance,
                _ when item.ItemType == ItemType.Event && item.EffectValue > 1 => ItemEffectType.PlayEffect,
                _ => item.Effect,
            };

            item.Material = item.VNum switch
            {
                1014 => MaterialType.CellaPowder,
                1015 => MaterialType.SoulGem,
                1016 => MaterialType.CompleteSoulGem,
                2774 => MaterialType.DimensionalForce,
                2748 => MaterialType.DimensiolithShard,
                1218 => MaterialType.EquipmentProtectionScroll,
                5369 => MaterialType.GoldenEquipmentProtectionScroll,
                2282 => MaterialType.AngelsFeather,
                1030 => MaterialType.FullMoonCrystal,
                2283 => MaterialType.GreenSoul,
                2284 => MaterialType.RedSoul,
                2285 => MaterialType.BlueSoul,
                2511 => MaterialType.DragonSkin,
                2512 => MaterialType.DragonBlood,
                2513 => MaterialType.DragonHeart,
                2630 => MaterialType.DragonGem,
                1363 => MaterialType.LowerSpProtectionScroll,
                1364 => MaterialType.HigherSpProtectionScroll,
                1027 => MaterialType.RiverSand,
                2514 => MaterialType.SmallRubyOfCompletion,
                2515 => MaterialType.SmallSapphireOfCompletion,
                2516 => MaterialType.SmallObsidianOfCompletion,
                2517 => MaterialType.SmallTopazOfCompletion,
                2518 => MaterialType.RubyOfCompletion,
                2519 => MaterialType.SapphireOfCompletion,
                2520 => MaterialType.ObsidianOfCompletion,
                2521 => MaterialType.TopazOfCompletion,
                1012 => MaterialType.SeedOfPower,
                _ => MaterialType.None,
            };

            // The production objects keep their minigame in the INDEX slot column, three tiers per game;
            // the typewriter and the memory table leave it 0 like the basic quarry.
            item.MinigameType = item.ItemType != ItemType.Minigame || item.MinilandObjectPoint == 0 ? null : item.VNum switch
            {
                3130 => MinigameType.Typewriter,
                3131 => MinigameType.MemoryTable,
                _ => (MinigameType)((byte)item.EquipmentSlot / 3),
            };

            // Each stone's description names the cards it perfects. None names Jajamaru or Chicken,
            // so their stones are unconfirmed.
            item.PerfectionMaterial = item.ItemType != ItemType.Specialist ? MaterialType.None : item.Morph switch
            {
                2 or 6 or 9 or 12 => MaterialType.SmallRubyOfCompletion,
                3 or 4 or 14 => MaterialType.SmallSapphireOfCompletion,
                5 or 11 or 15 => MaterialType.SmallObsidianOfCompletion,
                7 or 8 or 10 or 13 => MaterialType.SmallTopazOfCompletion,
                17 or 18 or 19 => MaterialType.RubyOfCompletion,
                20 or 21 or 22 => MaterialType.SapphireOfCompletion,
                23 or 24 or 25 => MaterialType.ObsidianOfCompletion,
                26 or 27 or 28 => MaterialType.TopazOfCompletion,
                _ => MaterialType.None,
            };

            item.UseSound = item.EffectValue switch
            {
                _ when item.Effect != ItemEffectType.PlayEffect => 0,
                859 => 1343,
                860 or 861 => 1344,
                875 => 1558,
                876 => 1559,
                877 or 878 => 1560,
                879 or 880 => 1561,
                _ => 0,
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
            // These items leave their BUFF row empty, so the card they grant is declared here.
            short grantedCard = item.Effect switch
            {
                ItemEffectType.SpeedBooster => 336,
                ItemEffectType.AncelloansBlessing => 121,
                ItemEffectType.HeatResistance => 340,
                ItemEffectType.FairyBooster => 131,
                _ => 0
            };
            if (grantedCard != 0 && item.BCards?.Any(b => b.Type == (byte)BCardType.CardType.Buff) != true)
            {
                item.BCards = [.. item.BCards ?? [], new BCardDto
                {
                    ItemVNum = item.VNum,
                    Type = (byte)BCardType.CardType.Buff,
                    SubType = 11,
                    FirstData = 100,
                    SecondData = grantedCard,
                }];
            }
        }
    }
}
