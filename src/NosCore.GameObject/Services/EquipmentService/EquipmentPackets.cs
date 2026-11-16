//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.EquipmentService;

public static class EquipmentPackets
{
    public static UpgradeRareSubPacket GetWeaponUpgradeRareSubPacket(this PlayerComponentBundle player)
    {
        var weapon = player.InventoryService.LoadBySlotAndType(EquipmentType.MainWeapon, NoscorePocketType.Wear);
        return new UpgradeRareSubPacket
        {
            Upgrade = weapon?.ItemInstance?.Upgrade ?? 0,
            Rare = (sbyte)(weapon?.ItemInstance?.Rare ?? 0)
        };
    }

    public static UpgradeRareSubPacket GetArmorUpgradeRareSubPacket(this PlayerComponentBundle player)
    {
        var armor = player.InventoryService.LoadBySlotAndType(EquipmentType.Armor, NoscorePocketType.Wear);
        return new UpgradeRareSubPacket
        {
            Upgrade = armor?.ItemInstance?.Upgrade ?? 0,
            Rare = (sbyte)(armor?.ItemInstance?.Rare ?? 0)
        };
    }

    public static EqPacket GenerateEq(this PlayerComponentBundle player)
    {
        return new EqPacket
        {
            VisualId = player.VisualId,
            Visibility = (byte)(player.Authority < AuthorityType.GameMaster ? 0 : 2),
            Gender = player.Gender,
            HairStyle = player.HairStyle,
            Haircolor = player.ShownHairColor(),
            ClassType = player.Class,
            EqSubPacket = player.GetEquipmentSubPacket(),
            WeaponUpgradeRarePacket = player.GetWeaponUpgradeRareSubPacket(),
            ArmorUpgradeRarePacket = player.GetArmorUpgradeRareSubPacket(),
            Size = player.Size
        };
    }

    public static EquipPacket GenerateEquipment(this PlayerComponentBundle player)
    {
        var inventory = player.InventoryService;

        EquipmentSubPacket? GenerateEquipmentSubPacket(EquipmentType eqType)
        {
            var eq = inventory.LoadBySlotAndType(eqType, NoscorePocketType.Wear);
            if (eq == null)
            {
                return null;
            }

            return new EquipmentSubPacket
            {
                EquipmentType = eqType,
                VNum = eq.ItemInstance.LookVNum(),
                Rare = eq.ItemInstance.Rare,
                Upgrade = (eq.ItemInstance.Item.IsColored ? eq.ItemInstance?.Design
                    : eq.ItemInstance.Upgrade) ?? 0,
                Unknown = 0
            };
        }

        return new EquipPacket
        {
            WeaponUpgradeRareSubPacket = player.GetWeaponUpgradeRareSubPacket(),
            ArmorUpgradeRareSubPacket = player.GetArmorUpgradeRareSubPacket(),
            Armor = GenerateEquipmentSubPacket(EquipmentType.Armor),
            WeaponSkin = GenerateEquipmentSubPacket(EquipmentType.WeaponSkin),
            SecondaryWeapon = GenerateEquipmentSubPacket(EquipmentType.SecondaryWeapon),
            Sp = GenerateEquipmentSubPacket(EquipmentType.Sp),
            Amulet = GenerateEquipmentSubPacket(EquipmentType.Amulet),
            Boots = GenerateEquipmentSubPacket(EquipmentType.Boots),
            CostumeHat = GenerateEquipmentSubPacket(EquipmentType.CostumeHat),
            CostumeSuit = GenerateEquipmentSubPacket(EquipmentType.CostumeSuit),
            Fairy = GenerateEquipmentSubPacket(EquipmentType.Fairy),
            Gloves = GenerateEquipmentSubPacket(EquipmentType.Gloves),
            Hat = GenerateEquipmentSubPacket(EquipmentType.Hat),
            MainWeapon = GenerateEquipmentSubPacket(EquipmentType.MainWeapon),
            Mask = GenerateEquipmentSubPacket(EquipmentType.Mask),
            Necklace = GenerateEquipmentSubPacket(EquipmentType.Necklace),
            Ring = GenerateEquipmentSubPacket(EquipmentType.Ring),
            Bracelet = GenerateEquipmentSubPacket(EquipmentType.Bracelet),
            WingSkin = GenerateEquipmentSubPacket(EquipmentType.WingSkin)
        };
    }
}
