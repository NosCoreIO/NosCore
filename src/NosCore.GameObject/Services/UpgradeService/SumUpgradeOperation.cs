//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.UpgradeService;

// Sum upgrade combines two wearables into one with combined upgrade level + summed
// elemental resistances. Modeled on OpenNos's WearableInstance.Sum (lines 609-665).
//
// Slot restriction: both items must occupy the SAME equipment slot AND that slot must be
// Boots or Gloves — those are the only equipment types Nostale lets you sum-resist on.
//
// Cost tables are indexed by source.Upgrade (NOT combined level — that's the success-rate
// table). Numbers ported from OpenNos:
//   upsuccess[combined]: { 100, 100, 85, 70, 50, 20 }
//   goldprice[source]:   { 1500, 3000, 6000, 12000, 24000, 48000 }
//   sand[source]:        { 5, 10, 15, 20, 25, 30 }
//
// On success: source.Upgrade += target.Upgrade + 1; resistances are summed.
// On failure: BOTH source and target are destroyed.
// Sand and gold are charged regardless.
[UsedImplicitly]
public sealed class SumUpgradeOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : UpgradeOperation(random, localizer)
{
    public const short SandVNum = 1027;

    private static readonly double[] SuccessRateByCombinedLevel = { 1.00, 1.00, 0.85, 0.70, 0.50, 0.20 };
    private static readonly long[] GoldCostBySourceLevel = { 1500, 3000, 6000, 12000, 24000, 48000 };
    private static readonly short[] SandCostBySourceLevel = { 5, 10, 15, 20, 25, 30 };

    public override UpgradePacketType Kind => UpgradePacketType.SumResistance;

    protected override Game18NConstString SuccessMessage => Game18NConstString.CombinationSuccessful;

    protected override Game18NConstString FailureMessage => Game18NConstString.CombinationFailed;

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        if (packet.InventoryType2 is null || packet.Slot2 is null)
        {
            return null;
        }

        var source = session.Character.InventoryService
            .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
        var target = session.Character.InventoryService
            .LoadBySlotAndType(packet.Slot2.Value, (NoscorePocketType)packet.InventoryType2.Value);

        if (source?.ItemInstance is not WearableInstance s || target?.ItemInstance is not WearableInstance t)
        {
            return null;
        }

        // Both must be the same slot type AND that slot must be Boots or Gloves.
        // Mirrors OpenNos WearableInstance.Sum lines 623-625.
        if (s.Item.EquipmentSlot != t.Item.EquipmentSlot
            || (s.Item.EquipmentSlot != EquipmentType.Boots
                && s.Item.EquipmentSlot != EquipmentType.Gloves))
        {
            return null;
        }

        if (s.Upgrade >= SandCostBySourceLevel.Length
            || s.Upgrade + t.Upgrade >= SuccessRateByCombinedLevel.Length)
        {
            return null;
        }

        return new UpgradeContext(
            Source: source,
            Target: target,
            GoldCost: GoldCostBySourceLevel[s.Upgrade],
            MaterialCosts: new[] { new MaterialCost(SandVNum, SandCostBySourceLevel[s.Upgrade]) },
            ExtraData: s.Upgrade + t.Upgrade);
    }

    protected override double GetSuccessRate(UpgradeContext ctx) =>
        SuccessRateByCombinedLevel[(int)ctx.ExtraData!];

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var source = (WearableInstance)ctx.Source.ItemInstance!;
        var target = (WearableInstance)ctx.Target!.ItemInstance!;

        source.DarkResistance = (short)((source.DarkResistance ?? 0) + (target.DarkResistance ?? 0));
        source.LightResistance = (short)((source.LightResistance ?? 0) + (target.LightResistance ?? 0));
        source.FireResistance = (short)((source.FireResistance ?? 0) + (target.FireResistance ?? 0));
        source.WaterResistance = (short)((source.WaterResistance ?? 0) + (target.WaterResistance ?? 0));
        source.Upgrade = (byte)(source.Upgrade + target.Upgrade + 1);
    }

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx)
    {
        session.Character.InventoryService.RemoveItemAmountFromInventory(1, ctx.Source.ItemInstanceId);
    }

    protected override void ConsumeFixedSlots(ClientSession session, UpgradeContext ctx)
    {
        // Target slot is consumed regardless of outcome.
        session.Character.InventoryService.RemoveItemAmountFromInventory(1, ctx.Target!.ItemInstanceId);
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        yield return ((InventoryItemInstance?)null).GeneratePocketChange(
            (PocketType)ctx.Target!.Type, ctx.Target.Slot);
        yield return outcome == UpgradeOutcome.Success
            ? ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot)
            : ((InventoryItemInstance?)null).GeneratePocketChange(
                (PocketType)ctx.Source.Type, ctx.Source.Slot);
    }
}
