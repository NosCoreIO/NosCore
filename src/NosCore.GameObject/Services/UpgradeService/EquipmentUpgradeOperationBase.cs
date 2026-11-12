//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Algorithm.EquipmentUpgradeService;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.UpgradeService;

// One roll against the item's upgrade level: the top of the range succeeds, the bottom fixes the
// item at its level, and what is left fails. A failure destroys the item unless a scroll is
// spent, in which case the item is kept and not fixed.
public abstract class EquipmentUpgradeOperationBase(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        IEquipmentUpgradeService equipmentUpgradeService)
    : UpgradeOperation(random, localizer)
{
    public const short CellonVNum = 1014;
    public const short SmallGemVNum = 1015;
    public const short FullGemVNum = 1016;
    public const short DimensionalForceVNum = 2774;
    public const short ShardVNum = 2748;
    public const short NormalScrollVNum = 1218;
    public const short GoldScrollVNum = 5369;

    protected abstract bool IsProtected { get; }

    protected virtual short ProtectionScrollVNum => NormalScrollVNum;

    protected virtual int GoldAndCellaDivisor => 1;

    protected override Game18NConstString SuccessMessage => Game18NConstString.UpgradeSuccessful;

    // Unprotected failure: the item was destroyed. Protected failure uses ProtectedSaveMessage
    // below; we reserve FailureMessage for the unprotected destruction path.
    protected override Game18NConstString FailureMessage => Game18NConstString.UpgradeFailed;

    // Fixed is the distinct upfix-band outcome (IsFixed=true, item preserved but locked).
    // OpenNos uses "UPGRADE_FIXED"; NosCore's enum ships with `ItemFixedLevel` ("Level X locked").
    protected override Game18NConstString FixedMessage => Game18NConstString.ItemFixedLevel;

    // ProtectedSave: scroll absorbed a failure roll. OpenNos sends different keys for say vs msg
    // ("SCROLL_PROTECT_USED" + "UPGRADE_FAILED_ITEM_SAVED"); NosCore overrides SayMessageFor for
    // that split. Default here returns the "item saved" copy used in the dialog.
    protected override Game18NConstString ProtectedSaveMessage => Game18NConstString.ItemSurvivedWithProtection;

    // Say vs Msg split for ProtectedSave: say = "scroll used", msg = "item saved".
    protected override Game18NConstString SayMessageFor(UpgradeOutcome outcome) =>
        outcome == UpgradeOutcome.ProtectedSave
            ? Game18NConstString.ProtectionScrollUsed
            : base.SayMessageFor(outcome);

    // OpenNos line 711: if IsFixed, emit ITEM_IS_FIXED say + shop_end 1 instead of silently
    // rejecting. That lets the player see why the operation did nothing.
    protected override IReadOnlyList<IPacket>? TryReject(ClientSession session, UpgradePacket packet)
    {
        var source = LoadSource(session, packet);
        if (source?.ItemInstance is not WearableInstance wearable || wearable.IsFixed != true)
        {
            return null;
        }
        return new IPacket[]
        {
            session.Character.GenerateSayi(SayColorType.Yellow, Game18NConstString.ItemFixedLevel),
            new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow },
        };
    }

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        var source = LoadSource(session, packet);
        if (source?.ItemInstance is not WearableInstance wearable)
        {
            return null;
        }

        if (wearable.Upgrade >= equipmentUpgradeService.MaxUpgrade)
        {
            return null;
        }

        var upgrade = wearable.Upgrade;
        var gemVNum = equipmentUpgradeService.GetGemType(upgrade) switch
        {
            UpgradeGemType.DimensionalForce => DimensionalForceVNum,
            UpgradeGemType.CompleteSoulGem => FullGemVNum,
            _ => SmallGemVNum,
        };

        var materials = new List<MaterialCost>
        {
            new(CellonVNum, (short)(equipmentUpgradeService.GetCellaCost(upgrade) / GoldAndCellaDivisor)),
            new(gemVNum, equipmentUpgradeService.GetGemCost(upgrade)),
        };
        var shards = equipmentUpgradeService.GetShardCost(upgrade);
        if (shards > 0)
        {
            materials.Add(new MaterialCost(ShardVNum, shards));
        }
        if (IsProtected)
        {
            materials.Add(new MaterialCost(ProtectionScrollVNum, 1));
        }

        return new UpgradeContext(
            Source: source,
            Target: null,
            GoldCost: equipmentUpgradeService.GetGoldCost(upgrade) / GoldAndCellaDivisor,
            MaterialCosts: materials);
    }

    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx)
    {
        var wearable = (WearableInstance)ctx.Source.ItemInstance;
        var percent = roll * 100;
        if (wearable.PityGauge >= equipmentUpgradeService.PityGaugeMaximum
            || percent >= 100 - equipmentUpgradeService.GetSuccessRate(wearable.Upgrade))
        {
            return UpgradeOutcome.Success;
        }

        if (percent < equipmentUpgradeService.GetFixedRate(wearable.Upgrade))
        {
            return UpgradeOutcome.Fixed;
        }

        return IsProtected ? UpgradeOutcome.ProtectedSave : UpgradeOutcome.Failure;
    }

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var wearable = (WearableInstance)ctx.Source.ItemInstance;
        wearable.Upgrade = (byte)(wearable.Upgrade + 1);
        wearable.PityGauge = 0;
    }

    // Every roll that does not succeed fills the item's gauge, whether the item is fixed, saved
    // by a scroll or destroyed; a full gauge makes the next attempt succeed.
    private void FillPityGauge(UpgradeContext ctx)
    {
        var wearable = (WearableInstance)ctx.Source.ItemInstance;
        wearable.PityGauge = (short)Math.Min(equipmentUpgradeService.PityGaugeMaximum,
            wearable.PityGauge + equipmentUpgradeService.GetPityGaugeGain(wearable.Upgrade));
    }

    protected override void ApplyProtectedSave(ClientSession session, UpgradeContext ctx) => FillPityGauge(ctx);

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx)
    {
        // Unprotected failure destroys the item entirely (matches OpenNos line 875).
        session.Character.InventoryService.RemoveItemAmountFromInventory(1, ctx.Source.ItemInstanceId);
    }

    protected override void ApplyFixed(ClientSession session, UpgradeContext ctx)
    {
        FillPityGauge(ctx);
        var wearable = (WearableInstance)ctx.Source.ItemInstance;
        wearable.IsFixed = true;
    }

    // OpenNos UpgradeItem effect pattern (WearableInstance.cs 834/841/848/863/878/885):
    //   - Success        → broadcast eff 3005 (green sparks)
    //   - Fixed          → broadcast eff 3004 (neutral poof)
    //   - ProtectedSave  → broadcast eff 3004 (scroll absorbs)
    //   - Failure        → no effect (silent destruction)
    protected override async Task EmitOutcomeEffectsAsync(ClientSession session, UpgradeContext ctx,
        UpgradeOutcome outcome, List<IPacket> playerPackets)
    {
        var effectId = outcome switch
        {
            UpgradeOutcome.Success => 3005,
            UpgradeOutcome.Fixed => 3004,
            UpgradeOutcome.ProtectedSave => 3004,
            _ => (int?)null,
        };
        if (effectId is null)
        {
            return;
        }
        await BroadcastEffectAsync(session, effectId.Value);
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        yield return RefreshSource(ctx, destroyed: outcome == UpgradeOutcome.Failure);
    }
}
