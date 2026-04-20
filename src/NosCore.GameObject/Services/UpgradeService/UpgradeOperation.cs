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
        }

        ConsumeMaterials(session, ctx, packets);
        ConsumeFixedSlots(session, ctx);
        await session.Character.RemoveGoldAsync(ctx.GoldCost, localizer).ConfigureAwait(false);

        packets.Add(new GuriPacket
        {
            Type = GuriPacketType.AfterSumming,
            Argument = 1,
            SecondArgument = 0,
            EntityId = session.Character.CharacterId,
            Value = (uint?)AnimationValueFor(outcome),
        });
        packets.Add(BuildResultMessage(session, outcome));
        packets.Add(new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow });
        packets.AddRange(BuildPocketRefresh(ctx, outcome));

        return packets;
    }

    // -- Hooks -----------------------------------------------------------------

    protected abstract UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet);

    protected abstract void ApplySuccess(UpgradeContext ctx);

    protected abstract void ApplyFailure(ClientSession session, UpgradeContext ctx);

    // Default Fixed handler is a no-op; subclasses that emit Fixed outcomes (EquipmentUpgrade)
    // override this to set IsFixed on the wearable.
    protected virtual void ApplyFixed(ClientSession session, UpgradeContext ctx) { }

    protected abstract Game18NConstString SuccessMessage { get; }

    protected abstract Game18NConstString FailureMessage { get; }

    // Override only if you emit Fixed outcomes; default mirrors FailureMessage so the call-site
    // doesn't need to special-case the absence.
    protected virtual Game18NConstString FixedMessage => FailureMessage;

    // Default 2-way roll using GetSuccessRate. Subclasses with 3-way roll bands
    // (EquipmentUpgrade with upfix/upfail) override this directly.
    protected virtual UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx) =>
        roll < GetSuccessRate(ctx) ? UpgradeOutcome.Success : UpgradeOutcome.Failure;

    // Convenience hook for the common 2-way case. Operations that override DetermineOutcome
    // wholesale don't need to implement this; throw to make that explicit.
    protected virtual double GetSuccessRate(UpgradeContext ctx) =>
        throw new NotImplementedException("Override DetermineOutcome or GetSuccessRate.");

    protected virtual int SuccessAnimationValue => 1324;

    protected virtual int FailureAnimationValue => 1332;

    // The "fix" animation is the same as failure on the wire (no special client-side art for it).
    protected virtual int FixedAnimationValue => FailureAnimationValue;

    protected virtual IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome) =>
        Enumerable.Empty<IPacket>();

    // Slots that are consumed regardless of outcome (e.g. sum's target slot).
    protected virtual void ConsumeFixedSlots(ClientSession session, UpgradeContext ctx) { }

    // -- Helpers ----------------------------------------------------------------

    private int AnimationValueFor(UpgradeOutcome outcome) => outcome switch
    {
        UpgradeOutcome.Success => SuccessAnimationValue,
        UpgradeOutcome.Fixed => FixedAnimationValue,
        _ => FailureAnimationValue,
    };

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

    private SayiPacket BuildResultMessage(ClientSession session, UpgradeOutcome outcome) => new()
    {
        VisualType = VisualType.Player,
        VisualId = session.Character.CharacterId,
        Type = outcome == UpgradeOutcome.Success ? SayColorType.Green : SayColorType.Red,
        Message = outcome switch
        {
            UpgradeOutcome.Success => SuccessMessage,
            UpgradeOutcome.Fixed => FixedMessage,
            _ => FailureMessage,
        },
    };
}
