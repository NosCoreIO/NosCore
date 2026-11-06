//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Inventory;

namespace NosCore.GameObject.Services.ItemInfoService;

public sealed class ItemInfoService(IDao<EquipmentOptionDto, Guid> equipmentOptionDao) : IItemInfoService
{
    private const byte AdventurerClass = 1;
    private const byte SwordsmanClass = 2;
    private const byte ArcherClass = 4;
    private const byte MagicianClass = 8;

    public IPacket? GenerateInfo(IItemInstance itemInstance)
    {
        return itemInstance switch
        {
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
}
