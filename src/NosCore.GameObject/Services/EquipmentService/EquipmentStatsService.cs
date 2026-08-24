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

/// <summary>
/// Adds up what is worn, following the same split as the original game.
///
/// ## Two values per stat, not one
///
/// Every piece carries the value <b>of the model</b> (the same on every copy) and the value
/// <b>of the copy</b> (the one that changes with upgrade and rarity). Both have to be added:
/// taking only one of them is the classic silent mistake - the weapon works, it deals damage, and it deals
/// systematically less than it should, with nothing to say so.
///
/// ## Tre profili separati
///
/// The main weapon feeds melee, the secondary feeds range, the armour feeds the defences.
/// They do not mix, and it is what lets an archer have different numbers from a
/// swordsman at the same level.
///
/// ## Gli altri pezzi
///
/// Hat, gloves, boots, necklaces and rings carry defences and resistances, not damage. The fairy
/// carries the elemental rate. Every slot is walked, skipping the three already counted and the
/// specialist card, which has a path of its own (see <c>SpecialistPointService</c>).
/// </summary>
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

        // The worn pieces' effects are gathered along the way and handed over whole to
        // whoever computes the stats: the fold by type and subtype is already written there and
        // duplicarla qui vorrebbe dire tenerne allineate due copie.
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

        // Everything else: hat, gloves, boots, necklaces, rings, bracelets. They carry defences
        // and resistances. The three already counted are skipped, and the specialist card, which has a
        // suo — sommarla qui la conterebbe due volte.
        for (byte slot = 0; slot < 16; slot++)
        {
            var piece = inventory.LoadBySlotAndType(slot, NoscorePocketType.Wear)?.ItemInstance
                as WearableInstance;
            if (piece?.Item == null)
            {
                continue;
            }

            // The effects are taken from EVERY piece, weapons and armour included: those are
            // excluded only from the flat stat sum below, which has already counted them
            // nei blocchi dedicati.
            CollectBCards(bcards, piece);

            // HP and MP too: any piece can carry them, weapons included, and the dedicated
            // blocks above do not look at them. They go before the skip, or precisely those
            // proprio quelli.
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

        // Array.Empty is a single shared instance: without it, "nothing worn" would produce
        // an empty list different from EquipmentStats.None's and the two would not compare
        // equal while describing the same thing.
        return stats with { BCards = bcards.Count == 0 ? System.Array.Empty<BCardDto>() : bcards };
    }

    /// <summary>
    /// A piece's effects: the model's and the copy's.
    ///
    /// The model always carries the same ones - it is the item itself. The copy can have its own,
    /// which in the game are the equipment options added with cells. Both have to be taken
    /// entrambi.
    /// </summary>
    private void CollectBCards(List<BCardDto> into, WearableInstance piece)
    {
        into.AddRange(cardCatalog.GetItemBCards(piece.ItemVNum));
    }

    /// <summary>
    /// The piece worn in that slot, if it is a wearable item.
    ///
    /// The equipment slot <b>is</b> the index in the pocket: that is how the game ties
    /// a piece to its place, and it is why a weapon in the bag does not count - it is in no
    /// slot.
    /// </summary>
    private static WearableInstance? Worn(ICharacterEntity character, EquipmentType slot) =>
        character.InventoryService?
            .LoadBySlotAndType((byte)slot, NoscorePocketType.Wear)?.ItemInstance as WearableInstance;

    /// <summary>
    /// Model value plus copy value. Either can be missing, and missing
    /// means zero, not "skip the piece".
    /// </summary>
    private static int Sum(short? instanceValue, short? itemValue) =>
        (instanceValue ?? 0) + (itemValue ?? 0);

    private static int Sum(byte? instanceValue, byte? itemValue) =>
        (instanceValue ?? 0) + (itemValue ?? 0);

    private static int Sum(short? instanceValue, short itemValue) =>
        (instanceValue ?? 0) + itemValue;

    private static int Sum(byte? instanceValue, byte itemValue) =>
        (instanceValue ?? 0) + itemValue;
}
