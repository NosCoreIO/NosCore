//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.EquipmentService;

// Main weapon feeds melee, secondary feeds range, the rest feed defences and resistances. The
// specialist card counts only while the character is transformed into it.
//
// Each stat is summed from both the model and the instance: the model carries the base, the
// instance carries what upgrade and rarity changed.
public sealed class EquipmentStatsService(
    BattleService.ICardCatalog cardCatalog,
    IDao<EquipmentOptionDto, Guid> equipmentOptionDao,
    SpecialistCardService.ISpecialistCardService specialistCardService)
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
                Element = fairy.Item?.Element ?? ElementType.Neutral,
                FairyWorn = true,
            };
        }

        if (character.UseSp
            && inventory.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear)?.ItemInstance
                is SpecialistInstance specialist)
        {
            stats = AddSpecialist(stats, specialist);
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
            bcards.AddRange(cardCatalog.GetItemBCards(piece.ItemVNum));
            if (piece.Item.EquipmentSlot is EquipmentType.Necklace or EquipmentType.Ring or EquipmentType.Bracelet)
            {
                var options = CellonOptions(piece);
                bcards.AddRange(options.Select(CellonBCard).OfType<BCardDto>());
                stats = stats with
                {
                    MpConsumptionDecrease = stats.MpConsumptionDecrease
                        + options.Where(o => o.Type == (byte)CellonType.MpConsumption).Sum(o => o.Value),
                };
            }

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

    // The card adds to both the melee and the ranged profile, since a transformed character
    // attacks with the card's skills whatever the weapon.
    private EquipmentStats AddSpecialist(EquipmentStats stats, SpecialistInstance specialist)
    {
        var item = specialist.Item;
        var points = specialistCardService.GetBonus(specialist);
        var minHit = Sum(specialist.DamageMinimum, item?.DamageMinimum) + points.Damage;
        var maxHit = Sum(specialist.DamageMaximum, item?.DamageMaximum) + points.Damage;
        var hitRate = Sum(specialist.HitRate, item?.HitRate) + points.HitRate;
        var criticalChance = Sum(specialist.CriticalLuckRate, item?.CriticalLuckRate) + points.CriticalChance;
        var criticalRate = Sum(specialist.CriticalRate, item?.CriticalRate) + points.CriticalRate;
        return stats with
        {
            MinHit = stats.MinHit + minHit,
            MaxHit = stats.MaxHit + maxHit,
            HitRate = stats.HitRate + hitRate,
            CriticalChance = stats.CriticalChance + criticalChance,
            CriticalRate = stats.CriticalRate + criticalRate,
            MinDistance = stats.MinDistance + minHit,
            MaxDistance = stats.MaxDistance + maxHit,
            DistanceRate = stats.DistanceRate + hitRate,
            DistanceCriticalChance = stats.DistanceCriticalChance + criticalChance,
            DistanceCriticalRate = stats.DistanceCriticalRate + criticalRate,
            CloseDefence = stats.CloseDefence + Sum(specialist.CloseDefence, item?.CloseDefence) + points.Defence,
            DistanceDefence = stats.DistanceDefence + Sum(specialist.DistanceDefence, item?.DistanceDefence)
                + points.Defence,
            MagicDefence = stats.MagicDefence + Sum(specialist.MagicDefence, item?.MagicDefence)
                + points.Defence + points.MagicDefence,
            DefenceDodge = stats.DefenceDodge + Sum(specialist.DefenceDodge, item?.DefenceDodge) + points.Dodge,
            DistanceDefenceDodge = stats.DistanceDefenceDodge
                + Sum(specialist.DistanceDefenceDodge, item?.DistanceDefenceDodge) + points.Dodge,
            FireResistance = stats.FireResistance + Sum(specialist.FireResistance, item?.FireResistance)
                + specialist.SpFire + points.Resistance,
            WaterResistance = stats.WaterResistance + Sum(specialist.WaterResistance, item?.WaterResistance)
                + specialist.SpWater + points.Resistance,
            LightResistance = stats.LightResistance + Sum(specialist.LightResistance, item?.LightResistance)
                + specialist.SpLight + points.Resistance,
            DarkResistance = stats.DarkResistance + Sum(specialist.DarkResistance, item?.DarkResistance)
                + specialist.SpDark + points.Resistance,
            SpecialistElementRate = stats.SpecialistElementRate
                + Sum(specialist.ElementRate, item?.ElementRate) + specialist.SpElement + points.ElementRate,
            Hp = stats.Hp + points.Hp,
            Mp = stats.Mp + points.Mp,
            SpecialistHpMpPercent = points.HpMpPercent,
        };
    }

    private List<EquipmentOptionDto> CellonOptions(WearableInstance jewel) =>
        jewel.EquipmentOptions ??= equipmentOptionDao.Where(o => o.WearableInstanceId == jewel.Id)?.ToList() ?? [];

    // MP consumption has no effect card: it is read straight off the gear when a skill is paid for.
    private static BCardDto? CellonBCard(EquipmentOptionDto option)
    {
        BCardEffect? effect = (CellonType)option.Type switch
        {
            CellonType.Hp => BCardEffect.MaxHpmpMaximumHpIncreased,
            CellonType.Mp => BCardEffect.MaxHpmpMaximumMpIncreased,
            CellonType.HpRecovery => BCardEffect.RecoveryHpRecoveryIncreased,
            CellonType.MpRecovery => BCardEffect.RecoveryMpRecoveryIncreased,
            CellonType.CriticalDamageDecrease => BCardEffect.CriticalDamageFromCriticalDecreased,
            _ => null,
        };
        return effect is { } e
            ? new BCardDto { Type = e.Type(), SubType = e.SubType(), FirstData = option.Value }
            : null;
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
