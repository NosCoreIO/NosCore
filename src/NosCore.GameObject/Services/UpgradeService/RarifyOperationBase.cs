//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
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

// Rarify is NOT a +1 increment — it's a probability-band reroll modeled on OpenNos's
// WearableInstance.RarifyItem (lines 266-440). The new Rare is selected by walking the
// probability table from the highest band downward and picking the first one the roll
// falls into. The output rare may be anywhere from -2 (cursed) up to 8 (heroic).
//
// Materials per attempt: Cellon (vnum 1014) × 5 + Gold 500. Protected variants additionally
// consume a Magic Pearl Scroll (vnum 1218); the scroll's only effect is to prevent the
// post-reroll Rare from dropping below the item's pre-reroll Rare when the source was
// already at Heroic-grade rarity (>= 8).
//
// Subclasses set Kind and IsProtected; everything else is shared.
public abstract class RarifyOperationBase(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : UpgradeOperation(random, localizer)
{
    public const short CellonVNum = 1014;
    public const short NormalScrollVNum = 1218;
    public const sbyte MaxRarity = 8;
    public const long BaseGoldCost = 500;
    public const short BaseCellonCost = 5;

    // Probability bands (out of 100) per OpenNos. The reroll picks the first band the roll
    // falls into, walking from rare8 down. Entries past index 0 (raren1, raren2) handle the
    // "negative rarity" outcomes when the source was a Drop reroll, which we don't model
    // here — for the Normal mode we only roll between rare0 and rare8.
    //   rare:   0   1   2   3   4   5   6   7   8
    private static readonly double[] BandProbabilities =
    {
        60, 40, 30, 15, 10, 5, 3, 2, 1,
    };

    protected abstract bool IsProtected { get; }

    protected override Game18NConstString SuccessMessage => Game18NConstString.RarityLevelIncreased;

    // Failure here means "the reroll didn't improve and may have downgraded".
    protected override Game18NConstString FailureMessage =>
        IsProtected ? Game18NConstString.RarityUnchangedProtectionScroll : Game18NConstString.RarityOnZero;

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        var source = session.Character.InventoryService
            .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
        if (source?.ItemInstance is not WearableInstance wearable)
        {
            return null;
        }

        // Negative-rarity (cursed) items are not rarifiable, and items at the heroic cap
        // can't be improved further. In OpenNos protected scrolls have a separate Heroic
        // path that we'd need IsHeroic mapped on the DTO to model — out of scope here.
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

    // Custom roll: walk the probability bands from the highest down to find the first one
    // the roll falls into. The roll is interpreted as a 0..100 cursor (the IRandomNumberSource
    // returns 0..1 so we multiply). Outcome:
    //   - new rare > original  → Success (rarity increased)
    //   - new rare = original  → Failure with "unchanged" framing (or scroll-protected)
    //   - new rare < original  → Failure (rarity decreased, item now generic)
    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx)
    {
        var state = (RarifyRollState)ctx.ExtraData!;
        state.NewRare = RollNewRare(roll, state.OriginalRare);

        return state.NewRare > state.OriginalRare ? UpgradeOutcome.Success : UpgradeOutcome.Failure;
    }

    private sbyte RollNewRare(double roll, sbyte originalRare)
    {
        var rnd = roll * 100.0;

        // Walk from highest rare down. First band the roll falls into wins.
        for (var rare = (sbyte)8; rare >= 0; rare--)
        {
            if (rnd < BandProbabilities[rare])
            {
                if (IsProtected && rare < originalRare)
                {
                    // Protected variant clamps the new rare so it never drops below the
                    // pre-reroll value. The scroll's only effect.
                    return originalRare;
                }
                return rare;
            }
        }

        // Roll exceeded every band — no improvement; resolve to "rare 0" (the legacy reset
        // semantic) for unprotected, or keep current rare for protected.
        return IsProtected ? originalRare : (sbyte)0;
    }

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var wearable = (WearableInstance)ctx.Source.ItemInstance!;
        wearable.Rare = ((RarifyRollState)ctx.ExtraData!).NewRare;
    }

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx)
    {
        // Failure path: write whatever the band-roll produced. For unprotected this may
        // demote the wearable; for protected the band-roll is clamped at original Rare so
        // the write is a no-op but still executes for consistency.
        var wearable = (WearableInstance)ctx.Source.ItemInstance!;
        wearable.Rare = ((RarifyRollState)ctx.ExtraData!).NewRare;
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        yield return ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot);
    }

    private sealed class RarifyRollState
    {
        public RarifyRollState(sbyte originalRare) => OriginalRare = originalRare;

        public sbyte OriginalRare { get; }

        public sbyte NewRare { get; set; }
    }
}
