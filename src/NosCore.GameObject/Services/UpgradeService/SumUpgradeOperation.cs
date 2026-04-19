//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.UpgradeService;

// Sum upgrade combines two wearable items: the new upgrade level becomes
// (source.Upgrade + target.Upgrade + 1) on success. Combined upgrade must stay below 6 —
// anything higher would land at 7+ which the game caps. The four elemental resistances of
// source and target are summed when the operation succeeds. Target is consumed either way;
// source is destroyed on failure. Sand (vnum 1027) and gold are charged regardless.
[UsedImplicitly]
public sealed class SumUpgradeOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : UpgradeOperation(random, localizer)
{
    public const short SandVNum = 1027;

    // Index by source.Upgrade + target.Upgrade. Combined level >= 6 is rejected upfront.
    // Numbers from the legacy server config; if you tune these update SumUpgradeOperationTests.
    private static readonly double[] SuccessRateByLevelSum = { 1.00, 1.00, 0.90, 0.80, 0.50, 0.20 };
    private static readonly long[] GoldCostByLevelSum = { 500, 1500, 3000, 6000, 12000, 24000 };
    private static readonly short[] SandCostByLevelSum = { 1, 2, 3, 4, 5, 6 };

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

        var combinedLevel = s.Upgrade + t.Upgrade;
        if (combinedLevel >= SuccessRateByLevelSum.Length)
        {
            return null;
        }

        return new UpgradeContext(
            Source: source,
            Target: target,
            GoldCost: GoldCostByLevelSum[combinedLevel],
            MaterialCosts: new[] { new MaterialCost(SandVNum, SandCostByLevelSum[combinedLevel]) },
            ExtraData: combinedLevel);
    }

    protected override double GetSuccessRate(UpgradeContext ctx) =>
        SuccessRateByLevelSum[(int)ctx.ExtraData!];

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

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, bool succeeded)
    {
        yield return ((InventoryItemInstance?)null).GeneratePocketChange(
            (PocketType)ctx.Target!.Type, ctx.Target.Slot);
        yield return succeeded
            ? ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot)
            : ((InventoryItemInstance?)null).GeneratePocketChange(
                (PocketType)ctx.Source.Type, ctx.Source.Slot);
    }
}
