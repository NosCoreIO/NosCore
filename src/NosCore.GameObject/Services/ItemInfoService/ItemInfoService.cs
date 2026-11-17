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
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.FairyBeadService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.GameObject.Services.SpecialistHolderService;
using NodaTime;
using NosCore.Packets.Enumerations;
using NosCore.GameObject.Services.SkillService;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Mates;

namespace NosCore.GameObject.Services.ItemInfoService;

public sealed class ItemInfoService(
    IDao<EquipmentOptionDto, Guid> equipmentOptionDao,
    List<SkillDto> skills,
    ISpExperienceService spExperienceService,
    ISpecialistPointService specialistPointService,
    ISpecialistCardService specialistCardService,
    ISpecialistHolderService specialistHolderService,
    IFairyBeadService fairyBeadService,
    IPartnerSkillCatalog partnerSkillCatalog,
    IClock clock) : IItemInfoService
{
    private const int ListedSpecialistSkills = 10;
    private const byte PartnerSpecialistSubType = 4;
    private const int PermanentHours = -1;

    private const byte AdventurerClass = 1;
    private const byte SwordsmanClass = 2;
    private const byte ArcherClass = 4;
    private const byte MagicianClass = 8;

    public IPacket? GenerateInfo(IItemInstance itemInstance)
    {
        return itemInstance switch
        {
            SpecialistInstance { Item.ItemSubType: PartnerSpecialistSubType } partnerCard => GeneratePartnerSpecialistInfo(partnerCard),
            SpecialistInstance specialist => GenerateSpecialistInfo(specialist),
            BoxInstance holder when specialistHolderService.IsSpecialistHolder(holder) => GenerateHolderInfo(holder),
            BoxInstance bead when fairyBeadService.IsFairyBead(bead) => GenerateFairyBeadInfo(bead),
            WearableInstance { Item.ItemType: ItemType.Shell } shell => GenerateShellInfo(shell),
            WearableInstance wearable => wearable.Item.EquipmentSlot switch
            {
                EquipmentType.MainWeapon or EquipmentType.SecondaryWeapon => GenerateWeaponInfo(wearable),
                EquipmentType.Armor => GenerateArmorInfo(wearable),
                EquipmentType.Gloves or EquipmentType.Boots or EquipmentType.Hat or EquipmentType.Mask => GenerateAccessoryInfo(wearable),
                EquipmentType.CostumeSuit => GenerateCostumeInfo(wearable),
                EquipmentType.CostumeHat or EquipmentType.WeaponSkin or EquipmentType.WingSkin => GenerateCostumeHatInfo(wearable),
                EquipmentType.Amulet => new EInfoAmuletPacket
                {
                    ItemVNum = wearable.ItemVNum,
                    LevelMinimum = wearable.Item.LevelMinimum,
                    RemainingTime = RemainingSeconds(wearable) * 10,
                    Price = wearable.Item.Price,
                },
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

    private EInfoWeaponPacket GenerateWeaponInfo(WearableInstance weapon)
    {
        var item = weapon.Item;
        var options = LoadShellOptions(weapon);
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
            ShellOptionCount = (byte)options.Count,
            ShellOptions = options,
            PityGauge = weapon.PityGauge,
        };
    }

    private EInfoArmorPacket GenerateArmorInfo(WearableInstance armor)
    {
        var item = armor.Item;
        var options = LoadShellOptions(armor);
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
            ShellOptionCount = (byte)options.Count,
            ShellOptions = options,
            PityGauge = armor.PityGauge,
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

    private EInfoCostumePacket GenerateCostumeInfo(WearableInstance costume)
    {
        var item = costume.Item;
        return new EInfoCostumePacket
        {
            ItemVNum = costume.ItemVNum,
            Rare = costume.Rare,
            Upgrade = costume.Upgrade,
            IsFixed = costume.IsFixed ?? false,
            LevelMinimum = item.LevelMinimum,
            CloseDefence = (short)(item.CloseDefence + (costume.CloseDefence ?? 0)),
            DistanceDefence = (short)(item.DistanceDefence + (costume.DistanceDefence ?? 0)),
            MagicDefence = (short)(item.MagicDefence + (costume.MagicDefence ?? 0)),
            DefenceDodge = (short)(item.DefenceDodge + (costume.DefenceDodge ?? 0)),
            Price = item.Price,
            TradeRestriction = TradeRestriction(costume),
            RemainingHours = RemainingHours(costume),
        };
    }

    private EInfoCostumeHatPacket GenerateCostumeHatInfo(WearableInstance costume)
    {
        var item = costume.Item;
        return new EInfoCostumeHatPacket
        {
            ItemVNum = costume.ItemVNum,
            LevelMinimum = item.LevelMinimum,
            CloseDefence = (short)(item.CloseDefence + (costume.CloseDefence ?? 0)),
            DistanceDefence = (short)(item.DistanceDefence + (costume.DistanceDefence ?? 0)),
            MagicDefence = (short)(item.MagicDefence + (costume.MagicDefence ?? 0)),
            DefenceDodge = (short)(item.DefenceDodge + (costume.DefenceDodge ?? 0)),
            FireResistance = (short)(item.FireResistance + (costume.FireResistance ?? 0)),
            WaterResistance = (short)(item.WaterResistance + (costume.WaterResistance ?? 0)),
            LightResistance = (short)(item.LightResistance + (costume.LightResistance ?? 0)),
            DarkResistance = (short)(item.DarkResistance + (costume.DarkResistance ?? 0)),
            Price = item.Price,
            TradeRestriction = TradeRestriction(costume),
            RemainingHours = RemainingHours(costume),
        };
    }

    private static TradeRestriction TradeRestriction(IItemInstance item) =>
        !item.Item.IsTradable || item.BoundCharacterId != null ? Packets.Enumerations.TradeRestriction.NotTradable
        : item.Item.RequireBinding ? Packets.Enumerations.TradeRestriction.TradableUntilEquipped
        : Packets.Enumerations.TradeRestriction.None;

    // A worn time-limited item counts down from its delete time; one never worn still has its whole validity.
    private long RemainingSeconds(IItemInstance item) => item.ItemDeleteTime is { } deleteTime
        ? Math.Max(0, (long)(deleteTime - clock.GetCurrentInstant()).TotalSeconds)
        : item.Item.ItemValidTime;

    private int RemainingHours(IItemInstance item) =>
        item.ItemDeleteTime == null && item.Item.ItemValidTime <= 0 ? PermanentHours : (int)(RemainingSeconds(item) / 3600);

    private EInfoSpecialistHolderPacket GenerateHolderInfo(BoxInstance holder)
    {
        if ((holder.HoldingVNum ?? 0) == 0)
        {
            return new EInfoSpecialistHolderPacket { ItemVNum = holder.ItemVNum };
        }

        var card = GenerateSpecialistInfo(specialistHolderService.TakeOut(holder));
        return new EInfoSpecialistHolderPacket
        {
            ItemVNum = holder.ItemVNum,
            IsFull = true,
            HoldingVNum = card.ItemVnum,
            SpLevel = card.SpLevel,
            Xp = card.Xp,
            XpMaximum = card.SpXpData,
            Upgrade = card.Upgrade,
            SlHit = card.SlHit,
            SlDefence = card.SlDefense,
            SlElement = card.SlElement,
            SlHp = card.SlHp,
            FreePoints = card.FreeSpPoints,
            SpStoneUpgrade = card.SpStoneUpgrade,
            FireResistance = card.FireResistance,
            WaterResistance = card.WaterResistance,
            LightResistance = card.LightResistance,
            DarkResistance = card.DarkResistance,
            AttackPoints = card.AttackPoints,
            DefencePoints = card.DefensePoints,
            ElementPoints = card.ElementPoints,
            HpMpPoints = card.HpMpPoints,
            FirePoints = card.SpFireResistance,
            WaterPoints = card.SpWaterResistance,
            LightPoints = card.SpLightResistance,
            DarkPoints = card.SpDarkResistance,
        };
    }

    private EInfoFairyBeadPacket GenerateFairyBeadInfo(BoxInstance bead)
    {
        if ((bead.HoldingVNum ?? 0) == 0)
        {
            return new EInfoFairyBeadPacket { ItemVNum = bead.ItemVNum };
        }

        var fairy = fairyBeadService.TakeOut(bead);
        return new EInfoFairyBeadPacket
        {
            ItemVNum = bead.ItemVNum,
            IsFull = true,
            HoldingVNum = fairy.ItemVNum,
            ElementRate = (short)(fairy.Item.ElementRate + (fairy.ElementRate ?? 0)),
        };
    }

    private PslInfoPacket GeneratePartnerSpecialistInfo(SpecialistInstance card)
    {
        var item = card.Item;
        var cardSkills = partnerSkillCatalog.CardSkills(card);
        return new PslInfoPacket
        {
            ItemVNum = card.ItemVNum,
            Element = (ElementType)item.Element,
            ElementRate = item.ElementRate,
            LevelMinimum = item.LevelMinimum,
            Speed = item.Speed,
            FireResistance = item.FireResistance,
            WaterResistance = item.WaterResistance,
            LightResistance = item.LightResistance,
            DarkResistance = item.DarkResistance,
            Skill1 = card.PartnerSkillDetails(cardSkills, 0),
            Skill2 = card.PartnerSkillDetails(cardSkills, 1),
            Skill3 = card.PartnerSkillDetails(cardSkills, 2),
            Upgrade = card.Upgrade,
            AttackPoints = card.SpDamage,
            DefencePoints = card.SpDefence,
            CriticalDamageReductionPoints = card.SpElement,
            HpMpPoints = card.SpHp,
            FirePoints = card.SpFire,
            WaterPoints = card.SpWater,
            LightPoints = card.SpLight,
            DarkPoints = card.SpDark,
        };
    }

    private EInfoShellPacket GenerateShellInfo(WearableInstance shell)
    {
        var options = LoadShellOptions(shell);
        return new EInfoShellPacket
        {
            ItemVNum = shell.ItemVNum,
            Level = shell.Upgrade,
            Rare = shell.Rare,
            Price = shell.Item.Price,
            ShellOptionCount = (byte)options.Count,
            ShellOptions = options,
        };
    }

    private List<EInfoShellOptionSubPacket> LoadShellOptions(WearableInstance piece) =>
        equipmentOptionDao.Where(o => o.WearableInstanceId == piece.Id)?
            .OrderBy(o => o.Level)
            .Select(o => new EInfoShellOptionSubPacket { Level = o.Level, Type = o.Type, Value = o.Value })
            .ToList() ?? [];

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
            .Where(s => s.IsSpecialist() && s.UpgradeType == item.Morph && s.LevelMinimum <= specialist.SpLevel)
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
            Speed = item.Speed,
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
