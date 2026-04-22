//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.UpgradeService;

// Common skeleton for every UpgradePacketType variant (sum, rarify, cellon, sp upgrade, ...).
//
// The flow is identical: validate slots, charge gold + materials, roll for an UpgradeOutcome,
// apply the outcome (success / failure / fixed), then emit the standard pocket refresh +
// animation + result message. Subclasses fill in the variant-specific bits via the hooks below.
//
// Outcome model is 3-way (Success / Failure / Fixed) to match OpenNos's equipment upgrade
// behavior where a "Fix" roll permanently locks the item without changing its upgrade level.
// The default DetermineOutcome implementation reduces to a binary success/failure roll —
// subclasses that need 3-way (EquipmentUpgrade) override it.
//
// Concurrency: per-session packet handling is already serialized in WorldPacketHandlingStrategy
// (see AcquirePacketLockAsync), so two upgrade packets from the same client cannot run in
// parallel. No additional locking is needed at this layer.
public abstract class UpgradeOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : IUpgradeOperation
{
    public abstract UpgradePacketType Kind { get; }

    public async Task<IReadOnlyList<IPacket>> ExecuteAsync(ClientSession session, UpgradePacket packet)
    {
        var earlyReject = TryReject(session, packet);
        if (earlyReject is not null)
        {
            return earlyReject;
        }

        var ctx = TryPrepareContext(session, packet);
        if (ctx is null)
        {
            return Array.Empty<IPacket>();
        }

        if (!CanAfford(session, ctx, out var rejection))
        {
            return new IPacket[] { rejection };
        }

        var packets = new List<IPacket>();
        var roll = random.NextDouble();
        var outcome = DetermineOutcome(roll, ctx);

        switch (outcome)
        {
            case UpgradeOutcome.Success:
                ApplySuccess(ctx);
                break;
            case UpgradeOutcome.Failure:
                ApplyFailure(session, ctx);
                break;
            case UpgradeOutcome.Fixed:
                ApplyFixed(session, ctx);
                break;
            case UpgradeOutcome.ProtectedSave:
                ApplyProtectedSave(session, ctx);
                break;
        }

        ConsumeMaterials(session, ctx, packets);
        ConsumeFixedSlots(session, ctx);
        await session.Character.RemoveGoldAsync(ctx.GoldCost, localizer).ConfigureAwait(false);

        await EmitOutcomeEffectsAsync(session, ctx, outcome, packets).ConfigureAwait(false);

        packets.Add(BuildSay(session, ctx, outcome, SayMessageFor(outcome)));
        packets.Add(BuildMsgi(ctx, outcome, MsgMessageFor(outcome)));
        packets.Add(new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow });
        packets.AddRange(BuildPocketRefresh(ctx, outcome));

        return packets;
    }

    // -- Hooks -----------------------------------------------------------------

    // Pre-operation validation that needs to emit a player-visible rejection message. Returns
    // null to continue to TryPrepareContext, or a packet list that is returned directly.
    // Equipment uses it for the "ItemIsFixed" path where OpenNos sends a say + shop_end.
    protected virtual IReadOnlyList<IPacket>? TryReject(ClientSession session, UpgradePacket packet) => null;

    protected abstract UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet);

    protected abstract void ApplySuccess(UpgradeContext ctx);

    protected abstract void ApplyFailure(ClientSession session, UpgradeContext ctx);

    // Default Fixed handler is a no-op; subclasses that emit Fixed outcomes (EquipmentUpgrade)
    // override this to set IsFixed on the wearable.
    protected virtual void ApplyFixed(ClientSession session, UpgradeContext ctx) { }

    // ProtectedSave is a failure roll absorbed by a scroll. Default: no item mutation; materials
    // and gold are still consumed by the outer skeleton.
    protected virtual void ApplyProtectedSave(ClientSession session, UpgradeContext ctx) { }

    protected abstract Game18NConstString SuccessMessage { get; }

    protected abstract Game18NConstString FailureMessage { get; }

    // Override only if you emit Fixed outcomes; default mirrors FailureMessage so the call-site
    // doesn't need to special-case the absence.
    protected virtual Game18NConstString FixedMessage => FailureMessage;

    // Override for operations that emit ProtectedSave; default falls back to FailureMessage.
    protected virtual Game18NConstString ProtectedSaveMessage => FailureMessage;

    // Default 2-way roll using GetSuccessRate. Subclasses with 3-way roll bands
    // (EquipmentUpgrade with upfix/upfail) override this directly.
    protected virtual UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx) =>
        roll < GetSuccessRate(ctx) ? UpgradeOutcome.Success : UpgradeOutcome.Failure;

    // Convenience hook for the common 2-way case. Operations that override DetermineOutcome
    // wholesale don't need to implement this; throw to make that explicit.
    protected virtual double GetSuccessRate(UpgradeContext ctx) =>
        throw new NotImplementedException("Override DetermineOutcome or GetSuccessRate.");

    protected virtual IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome) =>
        Enumerable.Empty<IPacket>();

    // Slots that are consumed regardless of outcome (e.g. sum's target slot).
    protected virtual void ConsumeFixedSlots(ClientSession session, UpgradeContext ctx) { }

    // Emit visual effects for the roll outcome. Equipment/Rarify broadcast `eff 3004/3005` to
    // the map and emit nothing to the player; Sum overrides to emit the `guri 19 ... 1324/1332`
    // sparkle animation to the player and broadcast a `guri 6` ground explosion. Default: nothing.
    protected virtual Task EmitOutcomeEffectsAsync(ClientSession session, UpgradeContext ctx,
        UpgradeOutcome outcome, List<IPacket> playerPackets) => Task.CompletedTask;

    // -- Helpers ----------------------------------------------------------------

    // OpenNos sends distinct say vs msg keys for the ProtectedSave case: say "SCROLL_PROTECT_USED"
    // + msg "UPGRADE_FAILED_ITEM_SAVED". The hooks below are split so subclasses can diverge.
    protected virtual Game18NConstString SayMessageFor(UpgradeOutcome outcome) => outcome switch
    {
        UpgradeOutcome.Success => SuccessMessage,
        UpgradeOutcome.Fixed => FixedMessage,
        UpgradeOutcome.ProtectedSave => ProtectedSaveMessage,
        _ => FailureMessage,
    };

    protected virtual Game18NConstString MsgMessageFor(UpgradeOutcome outcome) =>
        SayMessageFor(outcome);

    private static bool CanAfford(ClientSession session, UpgradeContext ctx, out InfoiPacket rejection)
    {
        rejection = null!;
        if (session.Character.Gold < ctx.GoldCost)
        {
            rejection = new InfoiPacket { Message = Game18NConstString.NotEnoughGold };
            return false;
        }

        foreach (var cost in ctx.MaterialCosts)
        {
            if (session.Character.InventoryService.CountItem(cost.VNum) < cost.Amount)
            {
                rejection = new InfoiPacket { Message = Game18NConstString.NotEnoughIngredients };
                return false;
            }
        }

        return true;
    }

    private static void ConsumeMaterials(ClientSession session, UpgradeContext ctx, List<IPacket> outPackets)
    {
        foreach (var cost in ctx.MaterialCosts)
        {
            var remaining = cost.Amount;
            var instances = session.Character.InventoryService
                .Where(kv => kv.Value.ItemInstance?.ItemVNum == cost.VNum)
                .Select(kv => kv.Value)
                .OrderBy(i => i.Slot)
                .ToList();

            foreach (var inst in instances)
            {
                if (remaining <= 0)
                {
                    break;
                }
                var taken = (short)Math.Min(remaining, inst.ItemInstance!.Amount);
                session.Character.InventoryService.RemoveItemAmountFromInventory(taken, inst.ItemInstanceId);
                remaining -= taken;
                outPackets.Add(inst.GeneratePocketChange((PocketType)inst.Type, inst.Slot));
            }
        }
    }

    protected virtual SayiPacket BuildSay(ClientSession session, UpgradeContext ctx,
        UpgradeOutcome outcome, Game18NConstString message) => new()
    {
        VisualType = VisualType.Player,
        VisualId = session.Character.CharacterId,
        Type = outcome == UpgradeOutcome.Success ? SayColorType.Green : SayColorType.Red,
        Message = message,
    };

    // Default MsgiPacket has no args; subclasses override for i18n messages that take a
    // parameter (e.g. Rarify's GambleSuccessful passes the new rare as %d).
    protected virtual MsgiPacket BuildMsgi(UpgradeContext ctx, UpgradeOutcome outcome,
        Game18NConstString message) => new()
    {
        Type = MessageType.Default,
        Message = message,
    };
}
