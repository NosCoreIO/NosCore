using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NosCore.Data;
using NosCore.Data.StaticEntities;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;

namespace NosCore.Parser.Parsers
{
    public class ItemParser
    {
        private readonly List<BCardDTO> _itemCards = new List<BCardDTO>();
        private readonly List<ItemDTO> _items = new List<ItemDTO>();

        private bool _itemAreaBegin;
        private int _itemCounter;

        public void Parse(string folder)
        {
            var fileId = $"{folder}\\Item.dat";

            using (var npcIdStream = new StreamReader(fileId, CodePagesEncodingProvider.Instance.GetEncoding(1252)))
            {
                var item = new ItemDTO();

                string line;
                while ((line = npcIdStream.ReadLine()) != null)
                {
                    var currentLine = line.Split('\t');

                    if (currentLine.Length > 3 && currentLine[1] == "VNUM")
                    {
                        _itemAreaBegin = true;
                        item.VNum = short.Parse(currentLine[2]);
                        item.Price = long.Parse(currentLine[3]);
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "END")
                    {
                        if (!_itemAreaBegin)
                        {
                            return;
                        }

                        if (DAOFactory.ItemDAO.FirstOrDefault(s => s.VNum == item.VNum) == null)
                        {
                            _items.Add(item);
                            _itemCounter++;
                        }

                        item = new ItemDTO();
                        _itemAreaBegin = false;
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "NAME")
                    {
                        item.Name = currentLine[2];
                    }
                    else if (currentLine.Length > 7 && currentLine[1] == "INDEX")
                    {
                        switch (Convert.ToByte(currentLine[2]))
                        {
                            case 4:
                                item.Type = InventoryType.Equipment;
                                break;

                            case 8:
                                item.Type = InventoryType.Equipment;
                                break;

                            case 9:
                                item.Type = InventoryType.Main;
                                break;

                            case 10:
                                item.Type = InventoryType.Etc;
                                break;

                            default:
                                item.Type = (InventoryType)Enum.Parse(typeof(InventoryType), currentLine[2]);
                                break;
                        }

                        item.ItemType = currentLine[3] != "-1"
                            ? (ItemType)Enum.Parse(typeof(ItemType), $"{(short)item.Type}{currentLine[3]}")
                            : ItemType.Weapon;
                        item.ItemSubType = Convert.ToByte(currentLine[4]);
                        item.EquipmentSlot = (EquipmentType)Enum.Parse(typeof(EquipmentType),
                            currentLine[5] != "-1" ? currentLine[5] : "0");

                        switch (item.VNum)
                        {
                            case 4101:
                            case 4102:
                            case 4103:
                            case 4104:
                            case 4105:
                                item.EquipmentSlot = 0;
                                break;

                            case 1906:
                                item.Morph = 2368;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 1907:
                                item.Morph = 2370;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 1965:
                                item.Morph = 2406;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 5008:
                                item.Morph = 2411;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 5117:
                                item.Morph = 2429;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 5152:
                                item.Morph = 2432;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 5173:
                                item.Morph = 2511;
                                item.Speed = 16;
                                item.WaitDelay = 3000;
                                break;

                            case 5196:
                                item.Morph = 2517;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 5226: // Invisible locomotion, only 5 seconds with booster
                                item.Morph = 1817;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 5228: // Invisible locoomotion, only 5 seconds with booster
                                item.Morph = 1819;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 5232:
                                item.Morph = 2520;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 5234:
                                item.Morph = 2522;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 5236:
                                item.Morph = 2524;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 5238:
                                item.Morph = 1817;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 5240:
                                item.Morph = 1819;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 5319:
                                item.Morph = 2526;
                                item.Speed = 22;
                                item.WaitDelay = 3000;
                                break;

                            case 5321:
                                item.Morph = 2528;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 5323:
                                item.Morph = 2530;
                                item.Speed = 22;
                                item.WaitDelay = 3000;
                                break;

                            case 5330:
                                item.Morph = 2928;
                                item.Speed = 22;
                                item.WaitDelay = 3000;
                                break;

                            case 5332:
                                item.Morph = 2930;
                                item.Speed = 14;
                                item.WaitDelay = 3000;
                                break;

                            case 5360:
                                item.Morph = 2932;
                                item.Speed = 22;
                                item.WaitDelay = 3000;
                                break;

                            case 5386:
                                item.Morph = 2934;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 5387:
                                item.Morph = 2936;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 5388:
                                item.Morph = 2938;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 5389:
                                item.Morph = 2940;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 5390:
                                item.Morph = 2942;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 5391:
                                item.Morph = 2944;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 5914:
                                item.Morph = 2513;
                                item.Speed = 14;
                                item.WaitDelay = 3000;
                                break;

                            case 5997:
                                item.Morph = 3679;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9054:
                                item.Morph = 2368;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 9055:
                                item.Morph = 2370;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 9058:
                                item.Morph = 2406;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 9065:
                                item.Morph = 2411;
                                item.Speed = 20;
                                item.WaitDelay = 3000;
                                break;

                            case 9070:
                                item.Morph = 2429;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9073:
                                item.Morph = 2432;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9078:
                                item.Morph = 2520;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9079:
                                item.Morph = 2522;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9080:
                                item.Morph = 2524;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9081:
                                item.Morph = 1817;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9082:
                                item.Morph = 1819;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9083:
                                item.Morph = 2526;
                                item.Speed = 22;
                                item.WaitDelay = 3000;
                                break;

                            case 9084:
                                item.Morph = 2528;
                                item.Speed = 22;
                                item.WaitDelay = 3000;
                                break;

                            case 9085:
                                item.Morph = 2930;
                                item.Speed = 22;
                                item.WaitDelay = 3000;
                                break;

                            case 9086:
                                item.Morph = 2928;
                                item.Speed = 22;
                                item.WaitDelay = 3000;
                                break;

                            case 9087:
                                item.Morph = 2930;
                                item.Speed = 14;
                                item.WaitDelay = 3000;
                                break;

                            case 9088:
                                item.Morph = 2932;
                                item.Speed = 22;
                                item.WaitDelay = 3000;
                                break;

                            case 9090:
                                item.Morph = 2934;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9091:
                                item.Morph = 2936;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9092:
                                item.Morph = 2938;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9093:
                                item.Morph = 2940;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9094:
                                item.Morph = 2942;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            case 9115:
                                item.Morph = 3679;
                                item.Speed = 21;
                                item.WaitDelay = 3000;
                                break;

                            default:
                                if (item.EquipmentSlot.Equals(EquipmentType.Amulet))
                                {
                                    switch (item.VNum)
                                    {
                                        case 4503:
                                            item.EffectValue = 4544;
                                            break;

                                        case 4504:
                                            item.EffectValue = 4294;
                                            break;

                                        case 282: // Red amulet
                                            item.Effect = 791;
                                            item.EffectValue = 3;
                                            break;

                                        case 283: // Blue amulet
                                            item.Effect = 792;
                                            item.EffectValue = 3;
                                            break;

                                        case 284: // Reinforcement amulet
                                            item.Effect = 793;
                                            item.EffectValue = 3;
                                            break;

                                        case 4264: // Heroic
                                            item.Effect = 794;
                                            item.EffectValue = 3;
                                            break;

                                        case 4262: // Random heroic
                                            item.Effect = 795;
                                            item.EffectValue = 3;
                                            break;

                                        default:
                                            item.EffectValue = Convert.ToInt16(currentLine[7]);
                                            break;
                                    }
                                }
                                else
                                {
                                    item.Morph = Convert.ToInt16(currentLine[7]);
                                }

                                break;
                        }
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "TYPE")
                    {
                        // currentLine[2] 0-range 2-range 3-magic
                        item.Class = item.EquipmentSlot == EquipmentType.Fairy ? (byte)15
                            : Convert.ToByte(currentLine[3]);
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "FLAG")
                    {
                        if (currentLine.Length > 5)
                            item.IsSoldable = currentLine[5] == "0";
                        if (currentLine.Length > 6)
                            item.IsDroppable = currentLine[6] == "0";
                        if (currentLine.Length > 7)
                            item.IsTradable = currentLine[7] == "0";
                        if (currentLine.Length > 8)
                            item.IsMinilandActionable = currentLine[8] == "1";
                        if (currentLine.Length > 9)
                            item.IsWarehouse = currentLine[9] == "1";
                        if (currentLine.Length > 10)
                            item.Flag9 = currentLine[10] == "1";
                        if (currentLine.Length > 11)
                            item.Flag1 = currentLine[11] == "1";
                        if (currentLine.Length > 12)
                            item.Flag2 = currentLine[12] == "1";
                        if (currentLine.Length > 13)
                            item.Flag3 = currentLine[13] == "1";
                        if (currentLine.Length > 14)
                            item.Flag4 = currentLine[14] == "1";
                        if (currentLine.Length > 15)
                            item.Flag5 = currentLine[15] == "1";
                        if (currentLine.Length > 16)
                            item.IsColored = currentLine[16] == "1";
                        if (currentLine.Length > 18)
                            item.Sex = currentLine[18] == "1" ? (byte)1 :
                            currentLine[17] == "1" ? (byte)2 : (byte)0;
                        //not used item.Flag6 = currentLine[19] == "1";
                        if (currentLine.Length > 20)
                            item.Flag6 = currentLine[20] == "1";
                        if (currentLine.Length > 21)
                        {
                            if (currentLine[21] == "1")
                            {
                                item.ReputPrice = item.Price;
                            }
                        }

                        if (currentLine.Length > 22)
                            item.IsHeroic = currentLine[22] == "1";
                        if (currentLine.Length > 23)
                            item.Flag7 = currentLine[23] == "1";
                        if (currentLine.Length > 24)
                            item.Flag8 = currentLine[24] == "1";
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "DATA")
                    {
                        switch (item.ItemType)
                        {
                            case ItemType.Weapon:
                                item.LevelMinimum = Convert.ToByte(currentLine[2]);
                                item.DamageMinimum = Convert.ToInt16(currentLine[3]);
                                item.DamageMaximum = Convert.ToInt16(currentLine[4]);
                                item.HitRate = Convert.ToInt16(currentLine[5]);
                                item.CriticalLuckRate = Convert.ToByte(currentLine[6]);
                                item.CriticalRate = Convert.ToInt16(currentLine[7]);
                                item.BasicUpgrade = Convert.ToByte(currentLine[10]);
                                item.MaximumAmmo = 100;
                                break;

                            case ItemType.Armor:
                                item.LevelMinimum = Convert.ToByte(currentLine[2]);
                                item.CloseDefence = Convert.ToInt16(currentLine[3]);
                                item.DistanceDefence = Convert.ToInt16(currentLine[4]);
                                item.MagicDefence = Convert.ToInt16(currentLine[5]);
                                item.DefenceDodge = Convert.ToInt16(currentLine[6]);
                                item.DistanceDefenceDodge = Convert.ToInt16(currentLine[6]);
                                item.BasicUpgrade = Convert.ToByte(currentLine[10]);
                                break;

                            case ItemType.Box:
                                switch (item.VNum)
                                {
                                    // add here your custom effect/effectvalue for box item, make
                                    // sure its unique for boxitems

                                    case 287:
                                        item.Effect = 69;
                                        item.EffectValue = 1;
                                        break;

                                    case 4240:
                                        item.Effect = 69;
                                        item.EffectValue = 2;
                                        break;

                                    case 4194:
                                        item.Effect = 69;
                                        item.EffectValue = 3;
                                        break;

                                    case 4106:
                                        item.Effect = 69;
                                        item.EffectValue = 4;
                                        break;

                                    case 185: // Hatus
                                    case 302: // Classic
                                    case 882: // Morcos
                                    case 942: // Calvina
                                    case 999: //Berios
                                        item.Effect = 999;
                                        break;

                                    default:
                                        item.Effect = Convert.ToInt16(currentLine[2]);
                                        item.EffectValue = Convert.ToInt32(currentLine[3]);
                                        item.LevelMinimum = Convert.ToByte(currentLine[4]);
                                        break;
                                }

                                break;

                            case ItemType.Fashion:
                                item.LevelMinimum = Convert.ToByte(currentLine[2]);
                                item.CloseDefence = Convert.ToInt16(currentLine[3]);
                                item.DistanceDefence = Convert.ToInt16(currentLine[4]);
                                item.MagicDefence = Convert.ToInt16(currentLine[5]);
                                item.DefenceDodge = Convert.ToInt16(currentLine[6]);
                                if (item.EquipmentSlot.Equals(EquipmentType.CostumeHat)
                                    || item.EquipmentSlot.Equals(EquipmentType.CostumeSuit))
                                {
                                    item.ItemValidTime = Convert.ToInt32(currentLine[13]) * 3600;
                                }

                                break;

                            case ItemType.Food:
                                item.Hp = Convert.ToInt16(currentLine[2]);
                                item.Mp = Convert.ToInt16(currentLine[4]);
                                break;

                            case ItemType.Jewelery:
                                switch (item.EquipmentSlot)
                                {
                                    case EquipmentType.Amulet:
                                        item.LevelMinimum = Convert.ToByte(currentLine[2]);
                                        if ((item.VNum > 4055 && item.VNum < 4061)
                                            || (item.VNum > 4172 && item.VNum < 4176))
                                        {
                                            item.ItemValidTime = 10800;
                                        }
                                        else if ((item.VNum > 4045 && item.VNum < 4056) || item.VNum == 967
                                            || item.VNum == 968)
                                        {
                                            // (item.VNum > 8104 && item.VNum < 8115) <= disaled for now
                                            // because doesn't work!
                                            item.ItemValidTime = 10800;
                                        }
                                        else
                                        {
                                            item.ItemValidTime = Convert.ToInt32(currentLine[3]) / 10;
                                        }

                                        break;
                                    case EquipmentType.Fairy:
                                        item.Element = Convert.ToByte(currentLine[2]);
                                        item.ElementRate = Convert.ToInt16(currentLine[3]);
                                        if (item.VNum <= 256)
                                        {
                                            item.MaxElementRate = 50;
                                        }
                                        else
                                        {
                                            if (item.ElementRate == 0)
                                            {
                                                if (item.VNum >= 800 && item.VNum <= 804)
                                                {
                                                    item.MaxElementRate = 50;
                                                }
                                                else
                                                {
                                                    item.MaxElementRate = 70;
                                                }
                                            }
                                            else if (item.ElementRate == 30)
                                            {
                                                if (item.VNum >= 884 && item.VNum <= 887)
                                                {
                                                    item.MaxElementRate = 50;
                                                }
                                                else
                                                {
                                                    item.MaxElementRate = 30;
                                                }
                                            }
                                            else if (item.ElementRate == 35)
                                            {
                                                item.MaxElementRate = 35;
                                            }
                                            else if (item.ElementRate == 40)
                                            {
                                                item.MaxElementRate = 70;
                                            }
                                            else if (item.ElementRate == 50)
                                            {
                                                item.MaxElementRate = 80;
                                            }
                                        }

                                        break;
                                    default:
                                        item.LevelMinimum = Convert.ToByte(currentLine[2]);
                                        item.MaxCellonLvl = Convert.ToByte(currentLine[3]);
                                        item.MaxCellon = Convert.ToByte(currentLine[4]);
                                        break;
                                }

                                break;

                            case ItemType.Event:
                                switch (item.VNum)
                                {
                                    case 1332:
                                        item.EffectValue = 5108;
                                        break;

                                    case 1333:
                                        item.EffectValue = 5109;
                                        break;

                                    case 1334:
                                        item.EffectValue = 5111;
                                        break;

                                    case 1335:
                                        item.EffectValue = 5107;
                                        break;

                                    case 1336:
                                        item.EffectValue = 5106;
                                        break;

                                    case 1337:
                                        item.EffectValue = 5110;
                                        break;

                                    case 1339:
                                        item.EffectValue = 5114;
                                        break;

                                    case 9031:
                                        item.EffectValue = 5108;
                                        break;

                                    case 9032:
                                        item.EffectValue = 5109;
                                        break;

                                    case 9033:
                                        item.EffectValue = 5011;
                                        break;

                                    case 9034:
                                        item.EffectValue = 5107;
                                        break;

                                    case 9035:
                                        item.EffectValue = 5106;
                                        break;

                                    case 9036:
                                        item.EffectValue = 5110;
                                        break;

                                    case 9038:
                                        item.EffectValue = 5114;
                                        break;

                                    // EffectItems aka. fireworks
                                    case 1581:
                                        item.EffectValue = 860;
                                        break;

                                    case 1582:
                                        item.EffectValue = 861;
                                        break;

                                    case 1585:
                                        item.EffectValue = 859;
                                        break;

                                    case 1983:
                                        item.EffectValue = 875;
                                        break;

                                    case 1984:
                                        item.EffectValue = 876;
                                        break;

                                    case 1985:
                                        item.EffectValue = 877;
                                        break;

                                    case 1986:
                                        item.EffectValue = 878;
                                        break;

                                    case 1987:
                                        item.EffectValue = 879;
                                        break;

                                    case 1988:
                                        item.EffectValue = 880;
                                        break;

                                    case 9044:
                                        item.EffectValue = 859;
                                        break;

                                    case 9059:
                                        item.EffectValue = 875;
                                        break;

                                    case 9060:
                                        item.EffectValue = 876;
                                        break;

                                    case 9061:
                                        item.EffectValue = 877;
                                        break;

                                    case 9062:
                                        item.EffectValue = 878;
                                        break;

                                    case 9063:
                                        item.EffectValue = 879;
                                        break;

                                    case 9064:
                                        item.EffectValue = 880;
                                        break;

                                    default:
                                        item.EffectValue = Convert.ToInt16(currentLine[7]);
                                        break;
                                }

                                break;

                            case ItemType.Special:
                                switch (item.VNum)
                                {
                                    case 1246:
                                    case 9020:
                                        item.Effect = 6600;
                                        item.EffectValue = 1;
                                        break;

                                    case 1247:
                                    case 9021:
                                        item.Effect = 6600;
                                        item.EffectValue = 2;
                                        break;

                                    case 1248:
                                    case 9022:
                                        item.Effect = 6600;
                                        item.EffectValue = 3;
                                        break;

                                    case 1249:
                                    case 9023:
                                        item.Effect = 6600;
                                        item.EffectValue = 4;
                                        break;

                                    case 5130:
                                    case 9072:
                                        item.Effect = 1006;
                                        break;

                                    case 1272:
                                    case 1858:
                                    case 9047:
                                        item.Effect = 1005;
                                        item.EffectValue = 10;
                                        break;

                                    case 1273:
                                    case 9024:
                                        item.Effect = 1005;
                                        item.EffectValue = 30;
                                        break;

                                    case 1274:
                                    case 9025:
                                        item.Effect = 1005;
                                        item.EffectValue = 60;
                                        break;

                                    case 1279:
                                    case 9029:
                                        item.Effect = 1007;
                                        item.EffectValue = 30;
                                        break;

                                    case 1280:
                                    case 9030:
                                        item.Effect = 1007;
                                        item.EffectValue = 60;
                                        break;

                                    case 1923:
                                    case 9056:
                                        item.Effect = 1007;
                                        item.EffectValue = 10;
                                        break;

                                    case 1275:
                                    case 1886:
                                    case 9026:
                                        item.Effect = 1008;
                                        item.EffectValue = 10;
                                        break;

                                    case 1276:
                                    case 9027:
                                        item.Effect = 1008;
                                        item.EffectValue = 30;
                                        break;

                                    case 1277:
                                    case 9028:
                                        item.Effect = 1008;
                                        item.EffectValue = 60;
                                        break;

                                    case 5060:
                                    case 9066:
                                        item.Effect = 1003;
                                        item.EffectValue = 30;
                                        break;

                                    case 5061:
                                    case 9067:
                                        item.Effect = 1004;
                                        item.EffectValue = 7;
                                        break;

                                    case 5062:
                                    case 9068:
                                        item.Effect = 1004;
                                        item.EffectValue = 1;
                                        break;

                                    case 5105:
                                        item.Effect = 651;
                                        break;

                                    case 5115:
                                        item.Effect = 652;
                                        break;

                                    case 1981:
                                        item.Effect = 34; // imagined number as for I = √(-1), complex z = a + bi
                                        break;

                                    case 1982:
                                        item.Effect = 6969; // imagined number as for I = √(-1), complex z = a + bi
                                        break;

                                    case 1894:
                                    case 1895:
                                    case 1896:
                                    case 1897:
                                    case 1898:
                                    case 1899:
                                    case 1900:
                                    case 1901:
                                    case 1902:
                                    case 1903:
                                        item.Effect = 789;
                                        item.EffectValue = item.VNum + 2152;
                                        break;

                                    case 4046:
                                    case 4047:
                                    case 4048:
                                    case 4049:
                                    case 4050:
                                    case 4051:
                                    case 4052:
                                    case 4053:
                                    case 4054:
                                    case 4055:
                                        item.Effect = 790;
                                        break;

                                    case 5119: // Speed booster
                                        item.Effect = 998;
                                        break;

                                    case 180: // attack amulet
                                        item.Effect = 932;
                                        break;

                                    case 181: // defense amulet
                                        item.Effect = 933;
                                        break;

                                    default:
                                        if ((item.VNum > 5891 && item.VNum < 5900)
                                            || (item.VNum > 9100 && item.VNum < 9109))
                                        {
                                            item.Effect =
                                                69; // imagined number as for I = √(-1), complex z = a + bi
                                        }
                                        else
                                        {
                                            item.Effect = Convert.ToInt16(currentLine[2]);
                                        }

                                        break;
                                }

                                switch (item.Effect)
                                {
                                    case 150:
                                    case 151:
                                        if (Convert.ToInt32(currentLine[4]) == 1)
                                        {
                                            item.EffectValue = 30000;
                                        }
                                        else if (Convert.ToInt32(currentLine[4]) == 2)
                                        {
                                            item.EffectValue = 70000;
                                        }
                                        else if (Convert.ToInt32(currentLine[4]) == 3)
                                        {
                                            item.EffectValue = 180000;
                                        }
                                        else
                                        {
                                            item.EffectValue = Convert.ToInt32(currentLine[4]);
                                        }

                                        break;

                                    case 204:
                                        item.EffectValue = 10000;
                                        break;

                                    case 305:
                                        item.EffectValue = Convert.ToInt32(currentLine[5]);
                                        item.Morph = Convert.ToInt16(currentLine[4]);
                                        break;

                                    default:
                                        item.EffectValue = item.EffectValue == 0 ? Convert.ToInt32(currentLine[4])
                                            : item.EffectValue;
                                        break;
                                }

                                item.WaitDelay = 5000;
                                break;

                            case ItemType.Magical:
                                if (item.VNum > 2059 && item.VNum < 2070)
                                {
                                    item.Effect = 10;
                                }
                                else
                                {
                                    item.Effect = Convert.ToInt16(currentLine[2]);
                                }

                                item.EffectValue = Convert.ToInt32(currentLine[4]);
                                break;

                            case ItemType.Specialist:

                                // item.isSpecialist = Convert.ToByte(currentLine[2]); item.Unknown = Convert.ToInt16(currentLine[3]);
                                item.ElementRate = Convert.ToInt16(currentLine[4]);
                                item.Speed = Convert.ToByte(currentLine[5]);
                                item.SpType = Convert.ToByte(currentLine[13]);

                                // item.Morph = Convert.ToInt16(currentLine[14]) + 1;
                                item.FireResistance = Convert.ToByte(currentLine[15]);
                                item.WaterResistance = Convert.ToByte(currentLine[16]);
                                item.LightResistance = Convert.ToByte(currentLine[17]);
                                item.DarkResistance = Convert.ToByte(currentLine[18]);

                                // item.PartnerClass = Convert.ToInt16(currentLine[19]);
                                item.LevelJobMinimum = Convert.ToByte(currentLine[20]);
                                item.ReputationMinimum = Convert.ToByte(currentLine[21]);

                                var elementdic = new Dictionary<int, int> { { 0, 0 } };
                                if (item.FireResistance != 0)
                                {
                                    elementdic.Add(1, item.FireResistance);
                                }

                                if (item.WaterResistance != 0)
                                {
                                    elementdic.Add(2, item.WaterResistance);
                                }

                                if (item.LightResistance != 0)
                                {
                                    elementdic.Add(3, item.LightResistance);
                                }

                                if (item.DarkResistance != 0)
                                {
                                    elementdic.Add(4, item.DarkResistance);
                                }

                                item.Element = (byte)elementdic.OrderByDescending(s => s.Value).First().Key;
                                if (elementdic.Count > 1 && elementdic.OrderByDescending(s => s.Value).First().Value
                                    == elementdic.OrderByDescending(s => s.Value).ElementAt(1).Value)
                                {
                                    item.SecondaryElement =
                                        (byte)elementdic.OrderByDescending(s => s.Value).ElementAt(1).Key;
                                }

                                // needs to be hardcoded
                                switch (item.VNum)
                                {
                                    case 901:
                                        item.Element = 1;
                                        break;

                                    case 903:
                                        item.Element = 2;
                                        break;

                                    case 906:
                                        item.Element = 3;
                                        break;

                                    case 909:
                                        item.Element = 3;
                                        break;
                                }

                                break;

                            case ItemType.Shell:

                                // item.ShellMinimumLevel = Convert.ToInt16(linesave[3]);
                                // item.ShellMaximumLevel = Convert.ToInt16(linesave[4]);
                                // item.ShellType = Convert.ToByte(linesave[5]); // 3 shells of each type
                                break;

                            case ItemType.Main:
                                item.Effect = Convert.ToInt16(currentLine[2]);
                                item.EffectValue = Convert.ToInt32(currentLine[4]);
                                break;

                            case ItemType.Upgrade:
                                item.Effect = Convert.ToInt16(currentLine[2]);
                                switch (item.VNum)
                                {
                                    // UpgradeItems (needed to be hardcoded)
                                    case 1218:
                                        item.EffectValue = 26;
                                        break;

                                    case 1363:
                                        item.EffectValue = 27;
                                        break;

                                    case 1364:
                                        item.EffectValue = 28;
                                        break;

                                    case 5107:
                                        item.EffectValue = 47;
                                        break;

                                    case 5207:
                                        item.EffectValue = 50;
                                        break;

                                    case 5369:
                                        item.EffectValue = 61;
                                        break;

                                    case 5519:
                                        item.EffectValue = 60;
                                        break;

                                    default:
                                        item.EffectValue = Convert.ToInt32(currentLine[4]);
                                        break;
                                }

                                break;

                            case ItemType.Production:
                                item.Effect = Convert.ToInt16(currentLine[2]);
                                item.EffectValue = Convert.ToInt32(currentLine[4]);
                                break;

                            case ItemType.Map:
                                item.Effect = Convert.ToInt16(currentLine[2]);
                                item.EffectValue = Convert.ToInt32(currentLine[4]);
                                break;

                            case ItemType.Potion:
                                item.Hp = Convert.ToInt16(currentLine[2]);
                                item.Mp = Convert.ToInt16(currentLine[4]);
                                break;

                            case ItemType.Snack:
                                item.Hp = Convert.ToInt16(currentLine[2]);
                                item.Mp = Convert.ToInt16(currentLine[4]);
                                break;

                            case ItemType.Teacher:
                                item.Effect = Convert.ToInt16(currentLine[2]);
                                item.EffectValue = Convert.ToInt32(currentLine[4]);

                                // item.PetLoyality = Convert.ToInt16(linesave[4]); item.PetFood = Convert.ToInt16(linesave[7]);
                                break;

                            case ItemType.Part:

                                // nothing to parse
                                break;

                            case ItemType.Sell:

                                // nothing to parse
                                break;

                            case ItemType.Quest2:

                                // nothing to parse
                                break;

                            case ItemType.Quest1:

                                // nothing to parse
                                break;

                            case ItemType.Ammo:

                                // nothing to parse
                                break;
                        }

                        if (item.Type == InventoryType.Miniland)
                        {
                            item.MinilandObjectPoint = int.Parse(currentLine[2]);
                            item.EffectValue = short.Parse(currentLine[8]);
                            item.Width = Convert.ToByte(currentLine[9]);
                            item.Height = Convert.ToByte(currentLine[10]);
                        }

                        if ((item.EquipmentSlot != EquipmentType.Boots &&
                                item.EquipmentSlot != EquipmentType.Gloves)
                            || item.Type != 0)
                        {
                            continue;
                        }

                        item.FireResistance = Convert.ToByte(currentLine[7]);
                        item.WaterResistance = Convert.ToByte(currentLine[8]);
                        item.LightResistance = Convert.ToByte(currentLine[9]);
                        item.DarkResistance = Convert.ToByte(currentLine[11]);
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "BUFF")
                    {
                        for (var i = 0; i < 5; i++)
                        {
                            var type = (byte)int.Parse(currentLine[2 + (5 * i)]);
                            if (type == 0 || type == 255)
                            {
                                continue;
                            }

                            var first = int.Parse(currentLine[3 + (5 * i)]);
                            var itemCard = new BCardDTO
                            {
                                ItemVNum = item.VNum,
                                Type = type,
                                SubType = (byte)(((int.Parse(currentLine[5 + (5 * i)]) + 1) * 10) + 1),
                                IsLevelScaled = Convert.ToBoolean(first % 4),
                                IsLevelDivided = first % 4 == 2,
                                FirstData = (short)((first > 0 ? first : -first) / 4),
                                SecondData = (short)(int.Parse(currentLine[4 + (5 * i)]) / 4),
                                ThirdData = (short)(int.Parse(currentLine[6 + (5 * i)]) / 4)
                            };
                            _itemCards.Add(itemCard);
                        }
                    }
                }

                DAOFactory.ItemDAO.InsertOrUpdate(_items);
                DAOFactory.BcardDAO.InsertOrUpdate(_itemCards);
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.ITEMS_PARSED),
                    _itemCounter));
            }
        }
    }
}