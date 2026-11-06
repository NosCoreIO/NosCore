//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NosCore.Algorithm.SpExperienceService;
using NosCore.Algorithm.SpecialistPointService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Inventory;

namespace NosCore.GameObject.Services.ItemInfoService;

public sealed class ItemInfoService(
    IDao<EquipmentOptionDto, Guid> equipmentOptionDao,
    List<SkillDto> skills,
    ISpExperienceService spExperienceService,
    ISpecialistPointService specialistPointService,
    ISpecialistCardService specialistCardService) : IItemInfoService
{
    private const int PyjamaSpecialistClass = 32;
    private const int ListedSpecialistSkills = 10;

    private const byte AdventurerClass = 1;
    private const byte SwordsmanClass = 2;
    private const byte ArcherClass = 4;
    private const byte MagicianClass = 8;

    public IPacket? GenerateInfo(IItemInstance itemInstance)
    {
        return itemInstance switch
        {
            SpecialistInstance specialist => GenerateSpecialistInfo(specialist),
            WearableInstance wearable => wearable.Item.EquipmentSlot switch
            {
                EquipmentType.MainWeapon or EquipmentType.SecondaryWeapon => GenerateWeaponInfo(wearable),
                EquipmentType.Armor => GenerateArmorInfo(wearable),
                EquipmentType.Gloves or EquipmentType.Boots => GenerateAccessoryInfo(wearable),
                EquipmentType.Necklace or EquipmentType.Ring or EquipmentType.Bracelet => GenerateJewelryInfo(wearable),
                EquipmentType.Fairy => new EInfoFairyPacket
                {
                    ItemVNum = wearable.ItemVNum,
                    Element = wearable.Item.Element,
                    ElementRate = (short)(wearable.Item.ElementRate + (wearable.ElementRate ?? 0)),
                },
                _ => null
            },
            _ => null
        };
    }

    private static EInfoWeaponPacket GenerateWeaponInfo(WearableInstance weapon)
    {
        var item = weapon.Item;
        return new EInfoWeaponPacket
        {
            Type = (item.EquipmentSlot, item.Class) switch
            {
                (EquipmentType.MainWeapon, ArcherClass) => EInfoPacketType.MainWeaponArcher,
                (EquipmentType.MainWeapon, MagicianClass) => EInfoPacketType.MainWeaponMagician,
                (EquipmentType.SecondaryWeapon, AdventurerClass or SwordsmanClass) => EInfoPacketType.MainWeaponArcher,
                _ => EInfoPacketType.WeaponDefault
            },
            ItemVNum = weapon.ItemVNum,
            Rare = weapon.Rare,
            Upgrade = weapon.Upgrade,
            IsFixed = weapon.IsFixed ?? false,
            LevelMinimum = item.LevelMinimum,
            DamageMinimum = (short)(item.DamageMinimum + (weapon.DamageMinimum ?? 0)),
            DamageMaximum = (short)(item.DamageMaximum + (weapon.DamageMaximum ?? 0)),
            HitRate = (short)(item.HitRate + (weapon.HitRate ?? 0)),
            CriticalLuckRate = (short)(item.CriticalLuckRate + (weapon.CriticalLuckRate ?? 0)),
            CriticalRate = (short)(item.CriticalRate + (weapon.CriticalRate ?? 0)),
            Ammo = weapon.Ammo ?? 0,
            MaximumAmmo = item.MaximumAmmo,
            Price = item.Price,
            ShellRarity = weapon.ShellRarity ?? 0,
            BoundCharacterId = weapon.BoundCharacterId ?? 0,
        };
    }

    private static EInfoArmorPacket GenerateArmorInfo(WearableInstance armor)
    {
        var item = armor.Item;
        return new EInfoArmorPacket
        {
            ItemVNum = armor.ItemVNum,
            Rare = armor.Rare,
            Upgrade = armor.Upgrade,
            IsFixed = armor.IsFixed ?? false,
            LevelMinimum = item.LevelMinimum,
            CloseDefence = (short)(item.CloseDefence + (armor.CloseDefence ?? 0)),
            DistanceDefence = (short)(item.DistanceDefence + (armor.DistanceDefence ?? 0)),
            MagicDefence = (short)(item.MagicDefence + (armor.MagicDefence ?? 0)),
            DefenceDodge = (short)(item.DefenceDodge + (armor.DefenceDodge ?? 0)),
            Price = item.Price,
            ShellRarity = armor.ShellRarity ?? 0,
            BoundCharacterId = armor.BoundCharacterId ?? 0,
        };
    }

    private static EInfoAccessoryPacket GenerateAccessoryInfo(WearableInstance accessory)
    {
        var item = accessory.Item;
        return new EInfoAccessoryPacket
        {
            ItemVNum = accessory.ItemVNum,
            LevelMinimum = item.LevelMinimum,
            CloseDefence = (short)(item.CloseDefence + (accessory.CloseDefence ?? 0)),
            DistanceDefence = (short)(item.DistanceDefence + (accessory.DistanceDefence ?? 0)),
            MagicDefence = (short)(item.MagicDefence + (accessory.MagicDefence ?? 0)),
            DefenceDodge = (short)(item.DefenceDodge + (accessory.DefenceDodge ?? 0)),
            FireResistance = (short)(item.FireResistance + (accessory.FireResistance ?? 0)),
            WaterResistance = (short)(item.WaterResistance + (accessory.WaterResistance ?? 0)),
            LightResistance = (short)(item.LightResistance + (accessory.LightResistance ?? 0)),
            DarkResistance = (short)(item.DarkResistance + (accessory.DarkResistance ?? 0)),
            Price = item.Price,
            Upgrade = accessory.Upgrade,
        };
    }

    private EInfoJewelryPacket GenerateJewelryInfo(WearableInstance jewel)
    {
        var options = equipmentOptionDao.Where(o => o.WearableInstanceId == jewel.Id)?
            .OrderBy(o => o.Level)
            .Select(o => new EInfoCellonOptionSubPacket { Type = o.Type, Level = o.Level, Value = o.Value })
            .ToList() ?? [];
        return new EInfoJewelryPacket
        {
            ItemVNum = jewel.ItemVNum,
            LevelMinimum = jewel.Item.LevelMinimum,
            MaxCellonLevel = jewel.Item.MaxCellonLvl,
            MaxCellon = jewel.Item.MaxCellon,
            CellonOptionCount = (byte)options.Count,
            Price = jewel.Item.Price,
            CellonOptions = options,
        };
    }

    public SlInfoPacket GenerateSpecialistInfo(SpecialistInstance specialist)
    {
        var item = specialist.Item;
        var cardSkills = skills
            .Where(s => s.Class >= PyjamaSpecialistClass && s.UpgradeType == item.Morph && s.LevelMinimum <= specialist.SpLevel)
            .OrderBy(s => s.SkillVNum)
            .ToList();
        // The first skill is the card's basic attack, which the window does not list.
        var listed = cardSkills.Skip(1)
            .Where(s => s.SkillVNum <= cardSkills[0].SkillVNum + ListedSpecialistSkills)
            .Take(ListedSpecialistSkills)
            .Select(s => s.SkillVNum.ToString(CultureInfo.InvariantCulture))
            .ToList();

        return new SlInfoPacket
        {
            InventoryType = PocketType.Equipment,
            ItemVnum = specialist.ItemVNum,
            ItemMorph = item.Morph,
            SpLevel = specialist.SpLevel,
            LevelJobMinimum = item.LevelJobMinimum,
            ReputationMinimum = item.ReputationMinimum,
            SpType = item.SpType,
            FireResistance = item.FireResistance,
            WaterResistance = item.WaterResistance,
            LightResistance = item.LightResistance,
            DarkResistance = item.DarkResistance,
            Xp = specialist.Xp ?? 0,
            SpXpData = spExperienceService.GetSpExperience(specialist.SpLevel, isSecondarySp: false),
            Skill = cardSkills.Count == 0 ? "-1" : string.Join('.', listed),
            TransportId = specialist.TransportId,
            FreeSpPoints = specialistCardService.GetFreePoints(specialist),
            SlHit = specialistPointService.GetAttackLevel(specialist.SlDamage),
            SlDefense = specialistPointService.GetDefenceLevel(specialist.SlDefence),
            SlElement = specialistPointService.GetElementLevel(specialist.SlElement),
            SlHp = specialistPointService.GetHpMpLevel(specialist.SlHp),
            Upgrade = specialist.Upgrade,
            IsSpDestroyed = specialist.Rare == -2,
            SpStoneUpgrade = specialist.SpStoneUpgrade,
            AttackPoints = specialist.SpDamage,
            DefensePoints = specialist.SpDefence,
            ElementPoints = specialist.SpElement,
            HpMpPoints = specialist.SpHp,
            SpFireResistance = specialist.SpFire,
            SpWaterResistance = specialist.SpWater,
            SpLightResistance = specialist.SpLight,
            SpDarkResistance = specialist.SpDark,
        };
    }
}
