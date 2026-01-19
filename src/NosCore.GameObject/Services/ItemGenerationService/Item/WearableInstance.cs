//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
            switch (Item.EquipmentSlot)
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
