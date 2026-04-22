//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Packets;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.UpgradeService;

// Rarify mirrors OpenNos WearableInstance.RarifyItem (Normal mode, lines 266-527).
//
// The reroll picks the first band a uniform [0, 80) roll falls into, walking from the
// highest rare (7) downward. Negative rares (raren1/raren2) and the zero-reroll band
// are disabled in Normal mode — OpenNos zeroes those thresholds at line 294-296 so they
// never match.
//
// Scroll protection makes any band that would not improve the item skip; if every band
// is skipped, the item is saved at its current rare instead of destroyed. Without scroll
// the same "no band matched" path destroys the item outright.
//
// Materials per attempt: Cellon (vnum 1014) × 5 + Gold 500. Protected variant additionally
// consumes a Magic Pearl Scroll (vnum 1218).
public abstract class RarifyOperationBase(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : UpgradeOperation(random, localizer)
{
    public const short CellonVNum = 1014;
    public const short NormalScrollVNum = 1218;
    public const sbyte MaxRarity = 8;
    public const long BaseGoldCost = 500;
    public const short BaseCellonCost = 5;

    // OpenNos RarifyItem cumulative thresholds (Normal mode, rnd in [0, 80)).
    // The Rare 8 band is only reachable via the heroic-scroll path OpenNos guards with
    // Item.IsHeroic — NosCore doesn't model IsHeroic yet, so rare 7 is the natural ceiling.
    //   rare:   7   6   5   4    3    2    1
    //   cap:    2   3   5   10   15   30   40
    // Rolls in [40, 80) match no band → Failure (destroy unprotected / save protected).
    private static readonly (sbyte Rare, double Threshold)[] NormalBands =
    {
        ((sbyte)7, 2),
        ((sbyte)6, 3),
        ((sbyte)5, 5),
        ((sbyte)4, 10),
        ((sbyte)3, 15),
        ((sbyte)2, 30),
        ((sbyte)1, 40),
    };

    protected abstract bool IsProtected { get; }

    // Trace-driven message ids (NosCore.Packets.Game18NConstString):
    //   147 GambleSuccessful               → "Gamble successful! Rarity level: %d"
    //   146 GambleItemDisappeared          → "The item was destroyed as the rarity level change failed!"
    //  1079 RarityUnchangedProtectionScroll → "The rarity level remains unchanged thanks to the protection scroll."
    // Previous implementation used 547 (Amulet-specific success copy) and 1398 (precondition
    // "must be rarity 0"), which made failures look like gate rejections post-gold-deduction.
    protected override Game18NConstString SuccessMessage => Game18NConstString.GambleSuccessful;

    // Unprotected failure: item destroyed. Protected variant routes through ProtectedSave instead
    // of Failure, so FailureMessage only needs the destroy copy.
    protected override Game18NConstString FailureMessage => Game18NConstString.GambleItemDisappeared;

    // Protected failure: scroll absorbs the destroy, item stays at its original rare. OpenNos
    // line 512-513 uses "RARIFY_FAILED_ITEM_SAVED"; NosCore's closest enum is
    // `GambleFailedButSurvived` for say and `RarityUnchangedProtectionScroll` for the dialog.
    protected override Game18NConstString ProtectedSaveMessage =>
        Game18NConstString.RarityUnchangedProtectionScroll;

    protected override Game18NConstString SayMessageFor(UpgradeOutcome outcome) =>
        outcome == UpgradeOutcome.ProtectedSave
            ? Game18NConstString.GambleFailedButSurvived
            : base.SayMessageFor(outcome);

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        var source = session.Character.InventoryService
            .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
        if (source?.ItemInstance is not WearableInstance wearable)
        {
            return null;
        }

        if (wearable.Rare < 0 || wearable.Rare >= MaxRarity)
        {
            return null;
        }

        var materials = new List<MaterialCost>
        {
            new(CellonVNum, BaseCellonCost),
        };
        if (IsProtected)
        {
            materials.Add(new MaterialCost(NormalScrollVNum, 1));
        }

        return new UpgradeContext(
            Source: source,
            Target: null,
            GoldCost: BaseGoldCost,
            MaterialCosts: materials,
            ExtraData: new RarifyRollState((sbyte)wearable.Rare));
    }

    // Walk the OpenNos bands descending from rare 7. First band the roll falls into whose
    // protection guard passes wins. If every band is skipped (scroll) or no band matches,
    // outcome is Failure (unprotected → destroy) or ProtectedSave (scroll absorbs).
    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx)
    {
        var state = (RarifyRollState)ctx.ExtraData!;
        var rnd = roll * 80.0;

        foreach (var (rare, threshold) in NormalBands)
        {
            if (rnd >= threshold)
            {
                continue;
            }
            // Scroll protection: skip any band that doesn't improve the current rare.
            if (IsProtected && rare <= state.OriginalRare)
            {
                continue;
            }
            state.NewRare = rare;
            return UpgradeOutcome.Success;
        }

        return IsProtected ? UpgradeOutcome.ProtectedSave : UpgradeOutcome.Failure;
    }

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var wearable = (WearableInstance)ctx.Source.ItemInstance!;
        wearable.Rare = ((RarifyRollState)ctx.ExtraData!).NewRare;
        // OpenNos line 413/422/431/…/SetRarityPoint re-rolls the rarity-driven stat bonuses
        // (defence for armor, damage/concentrate for weapons) to match the new rare tier.
        wearable.SetRarityPoint();
    }

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx)
    {
        // OpenNos WearableInstance.cs:507 — unprotected rarify failure destroys the item.
        session.Character.InventoryService.RemoveItemAmountFromInventory(1, ctx.Source.ItemInstanceId);
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        if (outcome == UpgradeOutcome.Failure)
        {
            yield return ((InventoryItemInstance?)null).GeneratePocketChange(
                (PocketType)ctx.Source.Type, ctx.Source.Slot);
            yield break;
        }
        yield return ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot);
    }

    // OpenNos effect pattern:
    //   - Success        → broadcast eff 3005 (Character.NotifyRarifyResult)
    //   - ProtectedSave  → broadcast eff 3004 (WearableInstance.cs:514)
    //   - Failure        → no effect (silent destruction)
    protected override async Task EmitOutcomeEffectsAsync(ClientSession session, UpgradeContext ctx,
        UpgradeOutcome outcome, List<IPacket> playerPackets)
    {
        var effectId = outcome switch
        {
            UpgradeOutcome.Success => 3005,
            UpgradeOutcome.ProtectedSave => 3004,
            _ => (int?)null,
        };
        if (effectId is null)
        {
            return;
        }
        await session.Character.MapInstance
            .SendPacketAsync(session.Character.GenerateEff(effectId.Value)).ConfigureAwait(false);
    }

    // Success message 147 "Gamble successful! Rarity level: %d" carries the new rare as a
    // long arg so the client renders the actual outcome rather than a generic confirmation.
    protected override MsgiPacket BuildMsgi(UpgradeContext ctx, UpgradeOutcome outcome,
        Game18NConstString message)
    {
        if (outcome != UpgradeOutcome.Success)
        {
            return base.BuildMsgi(ctx, outcome, message);
        }
        var newRare = ((RarifyRollState)ctx.ExtraData!).NewRare;
        return new MsgiPacket
        {
            Type = MessageType.Default,
            Message = message,
            Game18NArguments = new Game18NArguments(1) { (long)newRare },
        };
    }

    private sealed class RarifyRollState
    {
        public RarifyRollState(sbyte originalRare) => OriginalRare = originalRare;

        public sbyte OriginalRare { get; }

        public sbyte NewRare { get; set; }
    }
}
