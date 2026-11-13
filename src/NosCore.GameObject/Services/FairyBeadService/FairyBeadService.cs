//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.FairyBeadService;

public sealed class FairyBeadService(IItemGenerationService itemProvider) : IFairyBeadService
{
    public bool IsFairyBead(IItemInstance? item) =>
        item is BoxInstance { Item: { Effect: ItemEffectType.Holder } bead }
        && (HolderEffectValue)bead.EffectValue == HolderEffectValue.Fairy;

    public FairyStored? Store(PlayerComponentBundle character, short beadSlot, NoscorePocketType fairyPocket, short fairySlot)
    {
        if (character.InExchangeOrShop || fairyPocket != NoscorePocketType.Equipment)
        {
            return null;
        }

        var inventory = character.InventoryService;
        var beadItem = inventory.LoadBySlotAndType(beadSlot, NoscorePocketType.Equipment);
        var fairyItem = inventory.LoadBySlotAndType(fairySlot, fairyPocket);
        if (beadItem?.ItemInstance is not BoxInstance { HoldingVNum: null or 0 } bead
            || !IsFairyBead(bead)
            || fairyItem?.ItemInstance is not WearableInstance { Item.EquipmentSlot: EquipmentType.Fairy } fairy)
        {
            return null;
        }

        bead.HoldingVNum = fairy.ItemVNum;
        bead.ElementRate = fairy.ElementRate;
        bead.BoundCharacterId = fairy.BoundCharacterId;
        var remaining = inventory.RemoveItemAmountFromInventory(1, fairyItem.ItemInstanceId);
        return new FairyStored(fairyItem, remaining, beadItem);
    }

    public WearableInstance TakeOut(BoxInstance bead)
    {
        var fairy = (WearableInstance)itemProvider.Create(bead.HoldingVNum ?? throw new InvalidOperationException("The bead is empty."), 1);
        fairy.ElementRate = bead.ElementRate;
        fairy.BoundCharacterId = bead.BoundCharacterId;
        return fairy;
    }
}
