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
// The flow is identical: validate slots are present and not bound elsewhere, charge the player
// gold + materials, roll for success, mutate or destroy items, then emit the standard pocket
// refresh + animation + result message. Subclasses fill in the variant-specific bits via the
// abstract hooks below.
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
        var succeeded = random.NextDouble() < GetSuccessRate(ctx);
        if (succeeded)
        {
            ApplySuccess(ctx);
        }
        else
        {
            ApplyFailure(session, ctx);
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
            Value = (uint?)(succeeded ? SuccessAnimationValue : FailureAnimationValue),
        });
        packets.Add(BuildResultMessage(session, succeeded));
        packets.Add(new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow });
        packets.AddRange(BuildPocketRefresh(ctx, succeeded));

        return packets;
    }

    // -- Hooks -----------------------------------------------------------------

    protected abstract UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet);

    protected abstract double GetSuccessRate(UpgradeContext ctx);

    protected abstract void ApplySuccess(UpgradeContext ctx);

    protected abstract void ApplyFailure(ClientSession session, UpgradeContext ctx);

    protected abstract Game18NConstString SuccessMessage { get; }

    protected abstract Game18NConstString FailureMessage { get; }

    protected virtual int SuccessAnimationValue => 1324;

    protected virtual int FailureAnimationValue => 1332;

    protected virtual IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, bool succeeded) =>
        Enumerable.Empty<IPacket>();

    // Slots that are consumed regardless of outcome (e.g. sum's target slot, cellon's stone).
    // Default no-op; subclasses override when they have such slots.
    protected virtual void ConsumeFixedSlots(ClientSession session, UpgradeContext ctx) { }

    // -- Helpers ----------------------------------------------------------------

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

    private SayiPacket BuildResultMessage(ClientSession session, bool succeeded) => new()
    {
        VisualType = VisualType.Player,
        VisualId = session.Character.CharacterId,
        Type = succeeded ? SayColorType.Green : SayColorType.Red,
        Message = succeeded ? SuccessMessage : FailureMessage,
    };
}
