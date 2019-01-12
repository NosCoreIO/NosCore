//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Reactive.Subjects;
using NosCore.Data;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets;
using NosCore.Shared;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Services.ItemBuilder.Item
{
    public class WearableInstance : WearableInstanceDto, IItemInstance
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public WearableInstance(Item item)
        {
            Item = item;
            ItemVNum = item.VNum;
        }

        public WearableInstance()
        {
        }

        public bool IsBound => BoundCharacterId.HasValue && Item.ItemType != ItemType.Armor
            && Item.ItemType != ItemType.Weapon;

        public Item Item { get; set; }

        public object Clone()
        {
            return (WearableInstance) MemberwiseClone();
        }

        public Subject<RequestData<Tuple<IItemInstance, UseItemPacket>>> Requests { get; set; }

        public void SetRarityPoint()
        {
            switch (Item.EquipmentSlot)
            {
                case EquipmentType.MainWeapon:
                case EquipmentType.SecondaryWeapon:
                {
                    int point = CharacterHelper.Instance.RarityPoint(Rare,
                        Item.IsHeroic ? (short) (95 + Item.LevelMinimum) : Item.LevelMinimum);
                    Concentrate = 0;
                    HitRate = 0;
                    DamageMinimum = 0;
                    DamageMaximum = 0;
                    if (Rare >= 0)
                    {
                        for (int i = 0; i < point; i++)
                        {
                            int rndn = RandomFactory.Instance.RandomNumber(0, 3);
                            if (rndn == 0)
                            {
                                Concentrate++;
                                HitRate++;
                            }
                            else
                            {
                                DamageMinimum++;
                                DamageMaximum++;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i > Rare * 10; i--)
                        {
                            DamageMinimum--;
                            DamageMaximum--;
                        }
                    }
                }
                    break;

                case EquipmentType.Armor:
                {
                    int point = CharacterHelper.Instance.RarityPoint(Rare,
                        Item.IsHeroic ? (short) (95 + Item.LevelMinimum) : Item.LevelMinimum);
                    DefenceDodge = 0;
                    DistanceDefenceDodge = 0;
                    DistanceDefence = 0;
                    MagicDefence = 0;
                    CloseDefence = 0;
                    if (Rare < 0)
                    {
                        for (int i = 0; i > Rare * 10; i--)
                        {
                            DistanceDefence--;
                            MagicDefence--;
                            CloseDefence--;
                        }

                        return;
                    }

                    for (int i = 0; i < point; i++)
                    {
                        int rndn = RandomFactory.Instance.RandomNumber(0, 3);
                        if (rndn == 0)
                        {
                            DefenceDodge++;
                            DistanceDefenceDodge++;
                        }
                        else
                        {
                            DistanceDefence++;
                            MagicDefence++;
                            CloseDefence++;
                        }
                    }
                }
                    break;

                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.UNKNOWN_EQUIPMENTTYPE),
                        Item.EquipmentSlot);
                    break;
            }
        }
    }
}