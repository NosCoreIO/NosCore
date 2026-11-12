//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Shared.I18N;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.ItemGrantService;

public interface IItemGrantService
{
    // Reads the command's two optional values the way the item calls for: design and box rarity
    // for coloured items and boxes, upgrade, design and rarity for equipment, an amount otherwise.
    ItemGrantResult Grant(PlayerComponentBundle character, short vnum, short? designOrAmount, byte? upgrade);
}

public sealed class ItemGrantService(ILogger<ItemGrantService> logger, List<ItemDto> items,
        IOptions<WorldConfiguration> worldConfiguration, IItemGenerationService itemProvider,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage, IRarityPointService rarityPoints)
    : IItemGrantService
{
    public ItemGrantResult Grant(PlayerComponentBundle character, short vnum, short? designOrAmount, byte? upgradeArgument)
    {
        sbyte rare = 0;
        byte upgrade = 0;
        byte design = 0;
        short amount = 1;
        var iteminfo = items.Find(item => item.VNum == vnum);
        if (iteminfo == null)
        {
            return ItemGrantOutcome.UnknownItem;
        }

        if (iteminfo.Effect == ItemEffectType.Gold)
        {
            return ItemGrantOutcome.Refused;
        }

        if (iteminfo.IsColored || (iteminfo.Effect == ItemEffectType.BoxEffect))
        {
            if (designOrAmount.HasValue)
            {
                design = (byte)designOrAmount.Value;
            }

            rare = upgradeArgument.HasValue && (iteminfo.Effect == ItemEffectType.BoxEffect)
                ? (sbyte)upgradeArgument.Value : rare;
        }
        else if (iteminfo.Type == NoscorePocketType.Equipment)
        {
            if (upgradeArgument.HasValue)
            {
                if (iteminfo.EquipmentSlot != EquipmentType.Sp)
                {
                    upgrade = upgradeArgument.Value;
                }
                else
                {
                    design = upgradeArgument.Value;
                }

                if ((iteminfo.EquipmentSlot != EquipmentType.Sp) && (upgrade == 0)
                    && (iteminfo.BasicUpgrade != 0))
                {
                    upgrade = iteminfo.BasicUpgrade;
                }
            }

            if (designOrAmount.HasValue)
            {
                if (iteminfo.EquipmentSlot == EquipmentType.Sp)
                {
                    upgrade = (byte)designOrAmount.Value;
                }
                else if (iteminfo.EquipmentSlot == EquipmentType.Armor || iteminfo.EquipmentSlot == EquipmentType.MainWeapon || iteminfo.EquipmentSlot == EquipmentType.SecondaryWeapon)
                {
                    rare = (sbyte)designOrAmount.Value;
                }
            }
        }
        else if (designOrAmount.HasValue && !upgradeArgument.HasValue)
        {
            amount = designOrAmount.Value > worldConfiguration.Value.MaxItemAmount
                ? worldConfiguration.Value.MaxItemAmount : designOrAmount.Value;
        }

        var inv = character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(itemProvider.Create(
            vnum, amount, rare, upgrade, design), character.CharacterId));

        if (inv == null || inv.Count <= 0)
        {
            return ItemGrantOutcome.NotEnoughSpace;
        }

        var firstItem = inv[0];
        if (character.InventoryService.LoadBySlotAndType(firstItem.Slot, firstItem.Type)?.ItemInstance
            is WearableInstance wearable)
        {
            switch (wearable.Item.EquipmentSlot)
            {
                case EquipmentType.Armor:
                case EquipmentType.MainWeapon:
                case EquipmentType.SecondaryWeapon:
                    wearable.SetRarityPoint(rarityPoints);
                    break;
                case EquipmentType.Boots:
                case EquipmentType.Gloves:
                    wearable.FireResistance = (short)(wearable.Item.FireResistance * upgrade);
                    wearable.DarkResistance = (short)(wearable.Item.DarkResistance * upgrade);
                    wearable.LightResistance = (short)(wearable.Item.LightResistance * upgrade);
                    wearable.WaterResistance = (short)(wearable.Item.WaterResistance * upgrade);
                    break;
                default:
                    logger.LogDebug(logLanguage[LogLanguageKey.NO_SPECIAL_PROPERTIES_WEARABLE]);
                    break;
            }
        }

        return new ItemGranted(inv, amount);
    }
}
