//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.UpgradeService;

namespace NosCore.GameObject.Services.SpecialistHolderService;

public sealed class SpecialistHolderService(IItemGenerationService itemProvider) : ISpecialistHolderService
{
    public bool IsSpecialistHolder(IItemInstance? item) =>
        item is BoxInstance { Item: { Effect: ItemEffectType.Holder } holder }
        && (HolderEffectValue)holder.EffectValue is HolderEffectValue.SpecialistCard or HolderEffectValue.GoldenSpecialistCard;

    public bool CanStore(SpecialistInstance card) => card.Rare != SpUpgradeOperationBase.DestroyedSoulRare;

    public void Store(SpecialistInstance card, BoxInstance holder)
    {
        holder.HoldingVNum = card.ItemVNum;
        holder.Upgrade = card.Upgrade;
        holder.Design = card.Design;
        holder.BoundCharacterId = card.BoundCharacterId;
        holder.Xp = card.Xp;
        holder.SpLevel = card.SpLevel;
        holder.SpStoneUpgrade = card.SpStoneUpgrade;
        holder.SpDamage = card.SpDamage;
        holder.SpDefence = card.SpDefence;
        holder.SpElement = card.SpElement;
        holder.SpHp = card.SpHp;
        holder.SpFire = card.SpFire;
        holder.SpWater = card.SpWater;
        holder.SpLight = card.SpLight;
        holder.SpDark = card.SpDark;
        holder.SlDamage = card.SlDamage;
        holder.SlDefence = card.SlDefence;
        holder.SlElement = card.SlElement;
        holder.SlHp = card.SlHp;
    }

    public SpecialistStoreResult StoreFromEquipment(PlayerComponentBundle character, short cardSlot, short holderSlot)
    {
        if (character.InExchangeOrShop)
        {
            return new SpecialistStoreResult(SpecialistStoreOutcome.Ignored);
        }

        var cardItem = character.InventoryService.LoadBySlotAndType(cardSlot, NoscorePocketType.Equipment);
        var holderItem = character.InventoryService.LoadBySlotAndType(holderSlot, NoscorePocketType.Equipment);
        if (cardItem?.ItemInstance is not SpecialistInstance card
            || holderItem == null
            || !IsSpecialistHolder(holderItem.ItemInstance)
            || holderItem.ItemInstance is not BoxInstance { HoldingVNum: null or 0 } holder)
        {
            return new SpecialistStoreResult(SpecialistStoreOutcome.Ignored);
        }

        if (!CanStore(card))
        {
            return new SpecialistStoreResult(SpecialistStoreOutcome.CannotStore);
        }

        Store(card, holder);
        var remaining = character.InventoryService.RemoveItemAmountFromInventory(1, cardItem.ItemInstanceId);
        return new SpecialistStoreResult(SpecialistStoreOutcome.Stored, cardItem, remaining, holderItem);
    }

    public SpecialistInstance TakeOut(BoxInstance holder)
    {
        var card = (SpecialistInstance)itemProvider.Create(holder.HoldingVNum!.Value, 1, 0, holder.Upgrade, (byte)holder.Design);
        card.BoundCharacterId = holder.BoundCharacterId;
        card.Xp = holder.Xp;
        card.SpLevel = holder.SpLevel;
        card.SpStoneUpgrade = holder.SpStoneUpgrade;
        card.SpDamage = holder.SpDamage;
        card.SpDefence = holder.SpDefence;
        card.SpElement = holder.SpElement;
        card.SpHp = holder.SpHp;
        card.SpFire = holder.SpFire;
        card.SpWater = holder.SpWater;
        card.SpLight = holder.SpLight;
        card.SpDark = holder.SpDark;
        card.SlDamage = holder.SlDamage;
        card.SlDefence = holder.SlDefence;
        card.SlElement = holder.SlElement;
        card.SlHp = holder.SlHp;
        return card;
    }
}
