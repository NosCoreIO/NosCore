//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Data.Enumerations.Items;
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
        IEquipmentUpgradeService equipmentUpgradeService, IItemCatalog itemCatalog)
    : UpgradeOperation(random, localizer)
{
    protected abstract bool IsProtected { get; }

    protected virtual MaterialType ProtectionScroll => MaterialType.EquipmentProtectionScroll;

    protected virtual int GoldAndCellaDivisor => 1;

    protected override Game18NConstString SuccessMessage => Game18NConstString.UpgradeSuccessful;

    // Unprotected failure: the item was destroyed. Protected failure uses ProtectedSaveMessage
    // below; we reserve FailureMessage for the unprotected destruction path.
    protected override Game18NConstString FailureMessage => Game18NConstString.UpgradeFailed;

    // Fixed is the distinct upfix-band outcome (IsFixed=true, item preserved but locked).
    protected override Game18NConstString FixedMessage => Game18NConstString.ItemFixedLevel;

    // ProtectedSave: scroll absorbed a failure roll. The say and msg differ, so SayMessageFor is
    // overridden below; this is the "item saved" copy used in the dialog.
    protected override Game18NConstString ProtectedSaveMessage => Game18NConstString.ItemSurvivedWithProtection;

    // Say vs Msg split for ProtectedSave: say = "scroll used", msg = "item saved".
    protected override Game18NConstString SayMessageFor(UpgradeOutcome outcome) =>
        outcome == UpgradeOutcome.ProtectedSave
            ? Game18NConstString.ProtectionScrollUsed
            : base.SayMessageFor(outcome);

    // A fixed item gets an "item is fixed" say + shop_end 1 instead of a silent
    // rejection. That lets the player see why the operation did nothing.
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
        if (source?.ItemInstance is not WearableInstance wearable || !IsWeaponOrArmor(wearable))
        {
            return null;
        }

        if (wearable.Upgrade >= equipmentUpgradeService.MaxUpgrade)
        {
            return null;
        }

        var upgrade = wearable.Upgrade;
        var gem = equipmentUpgradeService.GetGemType(upgrade) switch
        {
            UpgradeGemType.DimensionalForce => MaterialType.DimensionalForce,
            UpgradeGemType.CompleteSoulGem => MaterialType.CompleteSoulGem,
            _ => MaterialType.SoulGem,
        };

        var materials = new List<MaterialCost>
        {
            new(itemCatalog.GetMaterialVNum(MaterialType.CellaPowder),
                (short)(equipmentUpgradeService.GetCellaCost(upgrade) / GoldAndCellaDivisor)),
            new(itemCatalog.GetMaterialVNum(gem), equipmentUpgradeService.GetGemCost(upgrade)),
        };
        var shards = equipmentUpgradeService.GetShardCost(upgrade);
        if (shards > 0)
        {
            materials.Add(new MaterialCost(itemCatalog.GetMaterialVNum(MaterialType.DimensiolithShard), shards));
        }
        if (IsProtected)
        {
            materials.Add(new MaterialCost(itemCatalog.GetMaterialVNum(ProtectionScroll), 1));
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
        session.Character.InventoryService.RemoveItemAmountFromInventory(1, ctx.Source.ItemInstanceId);
    }

    protected override void ApplyFixed(ClientSession session, UpgradeContext ctx)
    {
        FillPityGauge(ctx);
        var wearable = (WearableInstance)ctx.Source.ItemInstance;
        wearable.IsFixed = true;
    }

    protected override int? OutcomeEffect(UpgradeOutcome outcome) => outcome switch
    {
        UpgradeOutcome.Success => 3005,
        UpgradeOutcome.Fixed or UpgradeOutcome.ProtectedSave => 3004,
        _ => null,
    };

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        yield return RefreshSource(ctx, destroyed: outcome == UpgradeOutcome.Failure);
    }
}
