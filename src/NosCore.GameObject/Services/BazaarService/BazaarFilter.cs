//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Bazaar;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.BazaarService
{
    public readonly record struct BazaarFilter(BazaarListType Type, byte SubType, byte Level, byte Rare, byte Upgrade)
    {
        private const byte HeroicLevelFilter = 11;

        // Specialist sub-type filters 2 to 16 follow the bazaar's own order; from 17 on they are the morph plus one.
        private static readonly short[] SpecialistMorphs = { 10, 11, 2, 3, 13, 5, 12, 4, 7, 15, 6, 14, 9, 8, 1 };

        // The client's class drop-down lists the adventurer after the three specialist classes.
        private static readonly CharacterClassType[] ClassFilters =
        {
            CharacterClassType.Swordsman, CharacterClassType.Archer, CharacterClassType.Mage,
            CharacterClassType.Adventurer, CharacterClassType.MartialArtist
        };

        public bool Matches(ItemInstanceDto instance, ItemDto item, IReadOnlyDictionary<short, ItemDto> items)
        {
            var box = instance as BoxInstanceDto;
            return Type switch
            {
                BazaarListType.Weapon => IsEquipment(item, ItemType.Weapon) && MatchesClass(item) && MatchesItemLevel(item)
                    && MatchesRare(instance) && MatchesUpgrade(instance),
                BazaarListType.Armor => IsEquipment(item, ItemType.Armor) && MatchesClass(item) && MatchesItemLevel(item)
                    && MatchesRare(instance) && MatchesUpgrade(instance),
                BazaarListType.Equipment => IsEquipment(item, ItemType.Fashion) && MatchesFashionSlot(item) && MatchesItemLevel(item),
                BazaarListType.Jewelery => IsEquipment(item, ItemType.Jewelery) && MatchesJewelerySlot(item) && MatchesItemLevel(item),
                BazaarListType.Specialist => IsBox(item, 2) && box != null && MatchesSpecialist(box, items)
                    && MatchesBoxLevel(box) && MatchesUpgrade(instance),
                BazaarListType.Pet => IsBox(item, 0) && box != null && MatchesBoxLevel(box) && MatchesHolding(box),
                BazaarListType.Npc => IsBox(item, 1) && box != null && MatchesBoxLevel(box) && MatchesHolding(box),
                BazaarListType.Shell => item.Type == NoscorePocketType.Equipment && item.ItemType == ItemType.Shell
                    && (SubType == 0 || SubType == item.ItemSubType + 1) && MatchesRare(instance),
                BazaarListType.Main => item.Type == NoscorePocketType.Main && MatchesMainType(item),
                BazaarListType.Usable => item.Type == NoscorePocketType.Etc && MatchesUsableType(item),
                BazaarListType.Other => item.Type == NoscorePocketType.Equipment && item.ItemType == ItemType.Box && !item.Flag9,
                BazaarListType.Vehicle => item.ItemType == ItemType.Box && item.ItemSubType == 4 && box != null && MatchesHolding(box),
                _ => true
            };
        }

        private static bool IsEquipment(ItemDto item, ItemType type) =>
            item.Type == NoscorePocketType.Equipment && item.ItemType == type;

        private static bool IsBox(ItemDto item, byte subType) => IsEquipment(item, ItemType.Box) && item.ItemSubType == subType;

        private bool MatchesClass(ItemDto item) =>
            SubType == 0 || (SubType <= ClassFilters.Length && ((item.Class >> (byte)ClassFilters[SubType - 1]) & 1) == 1);

        private bool MatchesItemLevel(ItemDto item) =>
            Level == 0 || (Level == HeroicLevelFilter && item.IsHeroic) || InLevelBand(item.LevelMinimum);

        private bool MatchesBoxLevel(BoxInstanceDto box) => Level == 0 || InLevelBand(box.SpLevel);

        private bool InLevelBand(byte level) => level >= Level * 10 - 9 && level < Level * 10 + 1;

        private bool MatchesRare(ItemInstanceDto instance) => Rare == 0 || Rare == instance.Rare + 1;

        private bool MatchesUpgrade(ItemInstanceDto instance) => Upgrade == 0 || Upgrade == instance.Upgrade + 1;

        private bool MatchesHolding(BoxInstanceDto box) => SubType switch
        {
            0 => true,
            1 => IsEmpty(box),
            2 => !IsEmpty(box),
            _ => false
        };

        private bool MatchesSpecialist(BoxInstanceDto box, IReadOnlyDictionary<short, ItemDto> items)
        {
            switch (SubType)
            {
                case 0:
                    return true;
                case 1:
                    return IsEmpty(box);
            }

            if (box.HoldingVNum is not { } holding || IsEmpty(box) || !items.TryGetValue(holding, out var held))
            {
                return false;
            }

            var morph = SubType - 2 < SpecialistMorphs.Length ? SpecialistMorphs[SubType - 2] : SubType - 1;
            return held.Morph == morph;
        }

        private static bool IsEmpty(BoxInstanceDto box) => (box.HoldingVNum ?? 0) == 0;

        private bool MatchesFashionSlot(ItemDto item) => SubType == 0 || (BazaarEquipmentType)SubType switch
        {
            BazaarEquipmentType.Hat => item.EquipmentSlot == EquipmentType.Hat,
            BazaarEquipmentType.Mask => item.EquipmentSlot == EquipmentType.Mask,
            BazaarEquipmentType.Gloves => item.EquipmentSlot == EquipmentType.Gloves,
            BazaarEquipmentType.Boots => item.EquipmentSlot == EquipmentType.Boots,
            BazaarEquipmentType.CostumeSuit => item.EquipmentSlot == EquipmentType.CostumeSuit,
            BazaarEquipmentType.CostumeHat => item.EquipmentSlot == EquipmentType.CostumeHat,
            _ => false
        };

        private bool MatchesJewelerySlot(ItemDto item) => SubType == 0 || (BazaarJeweleryType)SubType switch
        {
            BazaarJeweleryType.Necklace => item.EquipmentSlot == EquipmentType.Necklace,
            BazaarJeweleryType.Ring => item.EquipmentSlot == EquipmentType.Ring,
            BazaarJeweleryType.Bracelet => item.EquipmentSlot == EquipmentType.Bracelet,
            BazaarJeweleryType.Fairy => item.EquipmentSlot == EquipmentType.Fairy,
            BazaarJeweleryType.Amulet => item.EquipmentSlot == EquipmentType.Amulet,
            _ => false
        };

        private bool MatchesMainType(ItemDto item) => SubType == 0 || (BazaarMainType)SubType switch
        {
            BazaarMainType.Main => item.ItemType == ItemType.Main,
            BazaarMainType.Upgrade => item.ItemType == ItemType.Upgrade,
            BazaarMainType.Production => item.ItemType == ItemType.Production,
            BazaarMainType.Special => item.ItemType == ItemType.Special,
            BazaarMainType.Potion => item.ItemType == ItemType.Potion,
            BazaarMainType.Event => item.ItemType == ItemType.Event,
            _ => false
        };

        private bool MatchesUsableType(ItemDto item) => SubType == 0 || (BazaarUsableType)SubType switch
        {
            BazaarUsableType.Food => item.ItemType == ItemType.Food,
            BazaarUsableType.Snack => item.ItemType == ItemType.Snack,
            BazaarUsableType.Magical => item.ItemType == ItemType.Magical,
            BazaarUsableType.Part => item.ItemType == ItemType.Part,
            BazaarUsableType.Teacher => item.ItemType == ItemType.Teacher,
            BazaarUsableType.Sell => item.ItemType == ItemType.Sell,
            _ => false
        };
    }
}
