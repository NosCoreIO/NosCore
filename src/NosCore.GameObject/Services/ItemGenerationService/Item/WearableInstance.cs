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

using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Helpers;
using NosCore.Shared.I18N;
using Serilog;
using System;

namespace NosCore.GameObject.Services.ItemGenerationService.Item
{
    public class WearableInstance : WearableInstanceDto, IItemInstance
    {
        private readonly ILogger _logger = null!;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage = null!;

        public WearableInstance(Item item, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            Id = Guid.NewGuid();
            Item = item;
            ItemVNum = item.VNum;
            _logger = logger;
            _logLanguage = logLanguage;
        }

        [Obsolete]
        public WearableInstance()
        {
        }

        public bool IsBound => BoundCharacterId.HasValue && (Item?.ItemType != ItemType.Armor)
            && (Item?.ItemType != ItemType.Weapon);

        public Item Item { get; set; } = null!;
        public object Clone()
        {
            return (WearableInstance)MemberwiseClone();
        }

        public void SetRarityPoint()
        {
            switch (Item!.EquipmentSlot)
            {
                case EquipmentType.MainWeapon:
                case EquipmentType.SecondaryWeapon:
                    {
                        var point = 0; //todo CharacterHelper.Instance.RarityPoint(Rare, Item.IsHeroic ? (short) (95 + Item.LevelMinimum) : Item.LevelMinimum);
                        Concentrate = 0;
                        HitRate = 0;
                        DamageMinimum = 0;
                        DamageMaximum = 0;
                        if (Rare >= 0)
                        {
                            for (var i = 0; i < point; i++)
                            {
                                var rndn = RandomHelper.Instance.RandomNumber(0, 3);
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
                            for (var i = 0; i > Rare * 10; i--)
                            {
                                DamageMinimum--;
                                DamageMaximum--;
                            }
                        }
                    }
                    break;

                case EquipmentType.Armor:
                    {
                        var point = 0; //todo CharacterHelper.Instance.RarityPoint(Rare, Item.IsHeroic ? (short) (95 + Item.LevelMinimum) : Item.LevelMinimum);
                        DefenceDodge = 0;
                        DistanceDefenceDodge = 0;
                        DistanceDefence = 0;
                        MagicDefence = 0;
                        CloseDefence = 0;
                        if (Rare < 0)
                        {
                            for (var i = 0; i > Rare * 10; i--)
                            {
                                DistanceDefence--;
                                MagicDefence--;
                                CloseDefence--;
                            }

                            return;
                        }

                        for (var i = 0; i < point; i++)
                        {
                            var rndn = RandomHelper.Instance.RandomNumber(0, 3);
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
                    _logger.Error(_logLanguage[LogLanguageKey.UNKNOWN_EQUIPMENTTYPE],
                        Item.EquipmentSlot);
                    break;
            }
        }
    }
}