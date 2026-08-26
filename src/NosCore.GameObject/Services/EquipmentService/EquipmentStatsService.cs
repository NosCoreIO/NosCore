//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Services.EquipmentService;

// Main weapon feeds melee, secondary feeds range, the rest feed defences and resistances. The
// specialist card goes through SpecialistPointService instead.
//
// Each stat is summed from both the model and the instance: the model carries the base, the
// instance carries what upgrade and rarity changed.
public sealed class EquipmentStatsService(BattleService.ICardCatalog cardCatalog)
    : IEquipmentStatsService, ISingletonService
{
    public EquipmentStats Resolve(IAliveEntity entity)
    {
        if (entity is not ICharacterEntity character)
        {
            return EquipmentStats.None;
        }

        var inventory = character.InventoryService;
        if (inventory == null)
        {
            return EquipmentStats.None;
        }

        var stats = EquipmentStats.None;

        // The effects are gathered along the way and handed over whole: the fold by type and
        // subtype already exists in BattleStatsProvider, and a second copy would drift from it.
        var bcards = new List<BCardDto>();

        var main = Worn(character, EquipmentType.MainWeapon);
        if (main != null)
        {
            stats = stats with
            {
                MinHit = stats.MinHit + Sum(main.DamageMinimum, main.Item?.DamageMinimum),
                MaxHit = stats.MaxHit + Sum(main.DamageMaximum, main.Item?.DamageMaximum),
                HitRate = stats.HitRate + Sum(main.HitRate, main.Item?.HitRate),
                CriticalChance = stats.CriticalChance + Sum(main.CriticalLuckRate, main.Item?.CriticalLuckRate),
                CriticalRate = stats.CriticalRate + Sum(main.CriticalRate, main.Item?.CriticalRate),
                MainWeaponUpgrade = main.Upgrade,
            };
        }

        var secondary = Worn(character, EquipmentType.SecondaryWeapon);
        if (secondary != null)
        {
            stats = stats with
            {
                MinDistance = stats.MinDistance + Sum(secondary.DamageMinimum, secondary.Item?.DamageMinimum),
                MaxDistance = stats.MaxDistance + Sum(secondary.DamageMaximum, secondary.Item?.DamageMaximum),
                DistanceRate = stats.DistanceRate + Sum(secondary.HitRate, secondary.Item?.HitRate),
                DistanceCriticalChance = stats.DistanceCriticalChance
                    + Sum(secondary.CriticalLuckRate, secondary.Item?.CriticalLuckRate),
                DistanceCriticalRate = stats.DistanceCriticalRate
                    + Sum(secondary.CriticalRate, secondary.Item?.CriticalRate),
                SecondaryWeaponUpgrade = secondary.Upgrade,
            };
        }

        var armour = Worn(character, EquipmentType.Armor);
        if (armour != null)
        {
            stats = stats with
            {
                CloseDefence = stats.CloseDefence + Sum(armour.CloseDefence, armour.Item?.CloseDefence),
                DistanceDefence = stats.DistanceDefence + Sum(armour.DistanceDefence, armour.Item?.DistanceDefence),
                MagicDefence = stats.MagicDefence + Sum(armour.MagicDefence, armour.Item?.MagicDefence),
                DefenceDodge = stats.DefenceDodge + Sum(armour.DefenceDodge, armour.Item?.DefenceDodge),
                DistanceDefenceDodge = stats.DistanceDefenceDodge
                    + Sum(armour.DistanceDefenceDodge, armour.Item?.DistanceDefenceDodge),
                ArmourUpgrade = armour.Upgrade,
            };
        }

        var fairy = Worn(character, EquipmentType.Fairy);
        if (fairy != null)
        {
            stats = stats with
            {
                ElementRate = stats.ElementRate + Sum(fairy.ElementRate, fairy.Item?.ElementRate),
            };
        }

        for (byte slot = 0; slot < 16; slot++)
        {
            var piece = inventory.LoadBySlotAndType(slot, NoscorePocketType.Wear)?.ItemInstance
                as WearableInstance;
            if (piece?.Item == null)
            {
                continue;
            }

            // Effects, HP and MP come off every piece including the weapons and armour handled
            // above; only the flat stats below skip them, having already been counted.
            CollectBCards(bcards, piece);

            stats = stats with
            {
                Hp = stats.Hp + piece.Item.Hp,
                Mp = stats.Mp + piece.Item.Mp,
            };

            var equipmentSlot = piece.Item.EquipmentSlot;
            if (equipmentSlot is EquipmentType.MainWeapon or EquipmentType.SecondaryWeapon
                or EquipmentType.Armor or EquipmentType.Sp or EquipmentType.Fairy)
            {
                continue;
            }

            stats = stats with
            {
                CloseDefence = stats.CloseDefence + Sum(piece.CloseDefence, piece.Item.CloseDefence),
                DistanceDefence = stats.DistanceDefence + Sum(piece.DistanceDefence, piece.Item.DistanceDefence),
                MagicDefence = stats.MagicDefence + Sum(piece.MagicDefence, piece.Item.MagicDefence),
                DefenceDodge = stats.DefenceDodge + Sum(piece.DefenceDodge, piece.Item.DefenceDodge),
                DistanceDefenceDodge = stats.DistanceDefenceDodge
                    + Sum(piece.DistanceDefenceDodge, piece.Item.DistanceDefenceDodge),
                FireResistance = stats.FireResistance + Sum(piece.FireResistance, piece.Item.FireResistance),
                WaterResistance = stats.WaterResistance + Sum(piece.WaterResistance, piece.Item.WaterResistance),
                LightResistance = stats.LightResistance + Sum(piece.LightResistance, piece.Item.LightResistance),
                DarkResistance = stats.DarkResistance + Sum(piece.DarkResistance, piece.Item.DarkResistance),
            };
        }

        // Array.Empty so "nothing worn" compares equal to EquipmentStats.None.
        return stats with { BCards = bcards.Count == 0 ? System.Array.Empty<BCardDto>() : bcards };
    }

    // The instance-level options added with cells are not reachable from a worn piece:
    // WearableInstance has no navigation to EquipmentOption.
    private void CollectBCards(List<BCardDto> into, WearableInstance piece)
    {
        into.AddRange(cardCatalog.GetItemBCards(piece.ItemVNum));
    }

    // The equipment slot is the index in the pocket, which is why a weapon in the bag does
    // not count.
    private static WearableInstance? Worn(ICharacterEntity character, EquipmentType slot) =>
        character.InventoryService?
            .LoadBySlotAndType((byte)slot, NoscorePocketType.Wear)?.ItemInstance as WearableInstance;

    private static int Sum(short? instanceValue, short? itemValue) =>
        (instanceValue ?? 0) + (itemValue ?? 0);

    private static int Sum(byte? instanceValue, byte? itemValue) =>
        (instanceValue ?? 0) + (itemValue ?? 0);

    private static int Sum(short? instanceValue, short itemValue) =>
        (instanceValue ?? 0) + itemValue;

    private static int Sum(byte? instanceValue, byte itemValue) =>
        (instanceValue ?? 0) + itemValue;
}
