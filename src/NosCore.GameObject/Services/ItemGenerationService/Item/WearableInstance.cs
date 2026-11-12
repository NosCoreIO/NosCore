//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Helpers;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.ItemGenerationService.Item
{
    public class WearableInstance : WearableInstanceDto, IItemInstance
    {
        private readonly ILogger<WearableInstance> _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public WearableInstance(Item item, ILogger<WearableInstance> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            Id = Guid.NewGuid();
            Item = item;
            ItemVNum = item.VNum;
            _logger = logger;
            _logLanguage = logLanguage;
        }


        public bool IsBound => BoundCharacterId.HasValue && (Item?.ItemType != ItemType.Armor)
            && (Item?.ItemType != ItemType.Weapon);

        public Item Item { get; set; }

        // The options live in their own table, so they are read on first use rather than with
        // the instance; null means not read yet.
        public List<EquipmentOptionDto>? EquipmentOptions { get; set; }
        public object Clone()
        {
            return (WearableInstance)MemberwiseClone();
        }

        // Heroic equipment counts its level past the 95 regular ones.
        private int RarityPoints(IRarityPointService rarityPoints) =>
            rarityPoints.GetRarityPoint((sbyte)Rare, (byte)(Item.IsHeroic ? 95 + Item.LevelMinimum : Item.LevelMinimum));

        public void SetRarityPoint(IRarityPointService rarityPoints)
        {
            switch (Item.EquipmentSlot)
            {
                case EquipmentType.MainWeapon:
                case EquipmentType.SecondaryWeapon:
                    {
                        var point = RarityPoints(rarityPoints);
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
                        var point = RarityPoints(rarityPoints);
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
                    _logger.LogError(_logLanguage[LogLanguageKey.UNKNOWN_EQUIPMENTTYPE],
                        Item.EquipmentSlot);
                    break;
            }
        }
    }
}
