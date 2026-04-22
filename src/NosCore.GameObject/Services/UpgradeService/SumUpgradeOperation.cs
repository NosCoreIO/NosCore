//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
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

    // OpenNos WearableInstance.cs:643-646 adds BOTH the target's accumulated (instance) resistance
    // AND its base-item resistance — a fresh pair of boots with Item.FireResistance=5 contributes
    // 5 to the fire total even if its accumulated instance resistance is 0.
    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var source = (WearableInstance)ctx.Source.ItemInstance!;
        var target = (WearableInstance)ctx.Target!.ItemInstance!;

        source.DarkResistance  = (short)((source.DarkResistance  ?? 0) + (target.DarkResistance  ?? 0) + target.Item.DarkResistance);
        source.LightResistance = (short)((source.LightResistance ?? 0) + (target.LightResistance ?? 0) + target.Item.LightResistance);
        source.FireResistance  = (short)((source.FireResistance  ?? 0) + (target.FireResistance  ?? 0) + target.Item.FireResistance);
        source.WaterResistance = (short)((source.WaterResistance ?? 0) + (target.WaterResistance ?? 0) + target.Item.WaterResistance);
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

    // OpenNos Sum effect pattern (WearableInstance.cs 651/658/662):
    //   - Player-only  : guri 19 1 <charId> 1324 (success) / 1332 (failure) — sparkle animation
    //   - Broadcast    : guri 6  1 <charId>                                — ground-burst explosion
    protected override async Task EmitOutcomeEffectsAsync(ClientSession session, UpgradeContext ctx,
        UpgradeOutcome outcome, List<IPacket> playerPackets)
    {
        playerPackets.Add(new GuriPacket
        {
            Type = GuriPacketType.AfterSumming,
            Argument = 1,
            SecondArgument = 0,
            EntityId = session.Character.CharacterId,
            Value = (uint)(outcome == UpgradeOutcome.Success ? 1324 : 1332),
        });
        await session.Character.MapInstance.SendPacketAsync(new GuriPacket
        {
            Type = GuriPacketType.CharacterAnimation,
            Argument = 1,
            SecondArgument = 0,
            EntityId = session.Character.CharacterId,
        }).ConfigureAwait(false);
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        yield return ((InventoryItemInstance?)null).GeneratePocketChange(
            (PocketType)ctx.Target!.Type, ctx.Target.Slot);
        if (outcome == UpgradeOutcome.Success)
        {
            yield return ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot);
            // OpenNos WearableInstance.cs:648 pdti 10 popup announcing the combined upgrade level.
            // Type=10 (ResistanceFused), RecipeAmount=1, Slot=27 are fixed values from OpenNos.
            var source = (WearableInstance)ctx.Source.ItemInstance!;
            yield return new PdtiPacket
            {
                Type = PdtiPacketType.ResistanceFused,
                Vnum = source.ItemVNum,
                RecipeAmount = 1,
                Slot = 27,
                ItemUpgrade = source.Upgrade,
                Rare = 0,
            };
        }
        else
        {
            yield return ((InventoryItemInstance?)null).GeneratePocketChange(
                (PocketType)ctx.Source.Type, ctx.Source.Slot);
        }
    }
}
