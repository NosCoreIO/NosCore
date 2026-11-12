//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Services.UpgradeService;

// The first costume keeps its effects and takes on the look of the second, which is destroyed.
[UsedImplicitly]
public sealed class CostumeFusionOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : UpgradeOperation(random, localizer), IItemOpenedUpgradeOperation
{
    public const long GoldCost = 1_000_000;

    public override UpgradePacketType Kind => UpgradePacketType.FusionItem;

    public override NpcDialogWindow Window => NpcDialogWindow.UpgradeScroll;

    public bool IsOpenedBy(ItemDto item) => item.Effect == ItemEffectType.CostumeFusion;

    protected override bool AnnouncesResult => false;

    protected override Game18NConstString SuccessMessage => default;

    protected override Game18NConstString FailureMessage => default;

    protected override IReadOnlyList<IPacket>? TryReject(ClientSession session, UpgradePacket packet)
    {
        if (LoadPair(session, packet) is not var (effects, design))
        {
            return null;
        }

        Game18NConstString? rejection =
            effects.AppearanceVNum is not null || design.AppearanceVNum is not null
                ? Game18NConstString.CannotUseItemWithChangedAppearance
                : effects.Item.ItemValidTime != 0 || design.Item.ItemValidTime != 0
                    ? Game18NConstString.BothMaterialsMustBePermanent
                    : effects.BoundCharacterId is null || design.BoundCharacterId is null
                        ? Game18NConstString.BothMaterialsMustBeBound
                        : null;
        return rejection is null ? null : new IPacket[]
        {
            new MsgiPacket { Type = MessageType.Default, Message = rejection.Value },
            new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow },
        };
    }

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        var inventory = session.Character.InventoryService;
        if (LoadPair(session, packet) is null
            || packet.Slot2 is not { } slot2 || packet.InventoryType2 is not { } type2
            || LoadSource(session, packet) is not { } source)
        {
            return null;
        }

        var fusionVNum = inventory.Values.Select(i => i.ItemInstance.Item)
            .Where(i => i.Effect == ItemEffectType.CostumeFusion)
            .MinBy(i => i.VNum)?.VNum ?? 0;
        return new UpgradeContext(
            Source: source,
            Target: inventory.LoadBySlotAndType(slot2, (NoscorePocketType)type2),
            GoldCost: GoldCost,
            MaterialCosts: new[] { new MaterialCost(fusionVNum, 1) });
    }

    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx) => UpgradeOutcome.Success;

    protected override void ApplySuccess(UpgradeContext ctx) =>
        ((WearableInstance)ctx.Source.ItemInstance).AppearanceVNum = ctx.RequiredTarget.ItemInstance.ItemVNum;

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx) { }

    protected override void ConsumeFixedSlots(ClientSession session, UpgradeContext ctx) =>
        session.Character.InventoryService.RemoveItemAmountFromInventory(1, ctx.RequiredTarget.ItemInstanceId);

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        yield return ((InventoryItemInstance?)null).GeneratePocketChange((PocketType)ctx.RequiredTarget.Type, ctx.RequiredTarget.Slot);
        yield return ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot);
    }

    // Two different costumes of the same kind, worn by the same sex.
    private static (WearableInstance Effects, WearableInstance Design)? LoadPair(ClientSession session,
        UpgradePacket packet)
    {
        if (packet.InventoryType2 is not { } designPocket || packet.Slot2 is not { } designSlot)
        {
            return null;
        }

        var inventory = session.Character.InventoryService;
        var effects = LoadSource(session, packet);
        var design = inventory.LoadBySlotAndType(designSlot, (NoscorePocketType)designPocket);
        if (effects?.ItemInstance is not WearableInstance e || design?.ItemInstance is not WearableInstance d
            || effects.Type != NoscorePocketType.Equipment || design.Type != NoscorePocketType.Equipment
            || effects.ItemInstanceId == design.ItemInstanceId
            || e.Item.ItemType != ItemType.Fashion
            || e.Item.EquipmentSlot is not (EquipmentType.CostumeSuit or EquipmentType.CostumeHat)
            || e.Item.EquipmentSlot != d.Item.EquipmentSlot
            || e.Item.Sex != d.Item.Sex)
        {
            return null;
        }

        return (e, d);
    }
}
