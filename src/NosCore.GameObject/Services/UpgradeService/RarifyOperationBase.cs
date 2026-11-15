//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.Algorithm.RarityPointService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Data.Enumerations.Items;
using System.Collections.Generic;
using System.Linq;
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
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.I18N;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Services.UpgradeService;

// The reroll picks the first band a uniform [0, 80) roll falls into, walking from the
// highest rare (7) downward. Negative rares (raren1/raren2) and the zero-reroll band
// are disabled in Normal mode, so they never match.
//
// Scroll protection makes any band that would not improve the item skip; if every band
// is skipped, the item is saved at its current rare instead of destroyed. Without scroll
// the same "no band matched" path destroys the item outright.
//
// Materials per attempt: 5 Cella Powder + 500 gold. The protected variant also consumes an
// Equipment Protection Scroll.
//
// A worn Blessing or Protection Amulet saves the item from destruction like the scroll does,
// but leaves the downgrade bands in play; the Blessing Amulet also widens every band by 10%.
// Each gamble spends one of the amulet's uses, and the amulet disappears with its last one.
//
// An Amulet for Increasing Rarity carried in the bag replaces the roll: the item gains one rarity
// level, cannot fail, and the amulet is used up. The champion one only works on champion gear,
// which it can raise to rare 8; the ordinary one only on other gear, up to rare 7.
public abstract class RarifyOperationBase(IRandomProvider random, IGameLanguageLocalizer localizer,
        IItemCatalog itemCatalog, IRarityPointService rarityPoints)
    : UpgradeOperation(random, localizer)
{
    public override NpcService? Service => NpcService.EquipmentRarify;

    public const sbyte MaxRarity = 8;
    public const long BaseGoldCost = 500;
    public const short BaseCellonCost = 5;
    private const double BlessingAmuletChanceFactor = 1.1;
    private const byte IntegerArgument = 4;

    // Cumulative thresholds for Normal mode, roll in [0, 80).
    // The Rare 8 band is only reachable through the heroic-scroll path, so rare 7 is the ceiling here.
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

    // Protected failure: scroll absorbs the destroy, item stays at its original rare.
    // `GambleFailedButSurvived` is the say line and `RarityUnchangedProtectionScroll` the dialog.
    protected override Game18NConstString ProtectedSaveMessage =>
        Game18NConstString.RarityUnchangedProtectionScroll;

    protected override Game18NConstString SayMessageFor(UpgradeOutcome outcome) =>
        outcome == UpgradeOutcome.ProtectedSave
            ? Game18NConstString.GambleFailedButSurvived
            : base.SayMessageFor(outcome);

    protected override IReadOnlyList<IPacket>? TryReject(ClientSession session, UpgradePacket packet)
    {
        if (WornBettingAmulet(session) is null)
        {
            return null;
        }

        Game18NConstString? refusal = IsProtected
            ? Game18NConstString.UnequipTheAmuletFirstThenYou
            : LoadSource(session, packet)?.ItemInstance is WearableInstance { Item.IsHeroic: true }
                ? Game18NConstString.YouAreWearingAnIncorrectAmulet
                : null;
        return refusal is { } message
            ? new IPacket[]
            {
                new MsgiPacket { Type = MessageType.Default, Message = message },
                new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow },
            }
            : null;
    }

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        var source = LoadSource(session, packet);
        if (source?.ItemInstance is not WearableInstance wearable || !IsWeaponOrArmor(wearable))
        {
            return null;
        }

        if (wearable.Rare < 0 || wearable.Rare >= MaxRarity)
        {
            return null;
        }

        var materials = new List<MaterialCost>
        {
            new(itemCatalog.GetMaterialVNum(MaterialType.CellaPowder), BaseCellonCost),
        };
        if (IsProtected)
        {
            materials.Add(new MaterialCost(itemCatalog.GetMaterialVNum(MaterialType.EquipmentProtectionScroll), 1));
        }

        return new UpgradeContext(
            Source: source,
            Target: null,
            GoldCost: BaseGoldCost,
            MaterialCosts: materials,
            ExtraData: new RarifyRollState((sbyte)wearable.Rare, WornBettingAmulet(session),
                CarriedRisingAmulet(session, wearable)));
    }

    // Walk the bands descending from rare 7. First band the roll falls into whose
    // protection guard passes wins. If every band is skipped (scroll) or no band matches,
    // outcome is Failure (unprotected → destroy) or ProtectedSave (scroll absorbs).
    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx)
    {
        var state = ctx.Data<RarifyRollState>();
        if (state.RisingAmulet is not null)
        {
            state.NewRare = (sbyte)(state.OriginalRare + 1);
            return UpgradeOutcome.Success;
        }

        var rnd = roll * 80.0;
        var chanceFactor = state.Amulet?.ItemInstance.Item.Effect == ItemEffectType.RedAmulet
            ? BlessingAmuletChanceFactor
            : 1;

        foreach (var (rare, threshold) in NormalBands)
        {
            if (rnd >= threshold * chanceFactor)
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

        return IsProtected || state.Amulet is not null ? UpgradeOutcome.ProtectedSave : UpgradeOutcome.Failure;
    }

    protected override void ConsumeFixedSlots(ClientSession session, UpgradeContext ctx)
    {
        var state = ctx.Data<RarifyRollState>();
        if (state.RisingAmulet is { } rising)
        {
            session.Character.InventoryService.RemoveItemAmountFromInventory(1, rising.ItemInstanceId);
        }
        else if (state.Amulet is { } amulet)
        {
            state.AmuletExpired = SpendAmuletUse(session, amulet);
        }
    }

    protected override async Task EmitOutcomeEffectsAsync(ClientSession session, UpgradeContext ctx,
        UpgradeOutcome outcome, List<IPacket> playerPackets)
    {
        await base.EmitOutcomeEffectsAsync(session, ctx, outcome, playerPackets);
        if (ctx.Data<RarifyRollState>().AmuletExpired)
        {
            await AnnounceExpiredAmuletAsync(session, playerPackets);
        }
    }

    private bool SavedByAmulet(UpgradeContext ctx, UpgradeOutcome outcome) =>
        outcome == UpgradeOutcome.ProtectedSave && !IsProtected && ctx.Data<RarifyRollState>().Amulet is not null;

    private static InventoryItemInstance? WornBettingAmulet(ClientSession session) =>
        WornAmulet(session, ItemEffectType.RedAmulet, ItemEffectType.BlueAmulet);

    private static InventoryItemInstance? CarriedRisingAmulet(ClientSession session, WearableInstance wearable)
    {
        var (effect, ceiling) = wearable.Item.IsHeroic
            ? (ItemEffectType.HeroicRarityIncreaseAmulet, MaxRarity)
            : (ItemEffectType.RarityIncreaseAmulet, (sbyte)(MaxRarity - 1));
        return wearable.Rare < ceiling
            ? session.Character.InventoryService.GetAll()
                .FirstOrDefault(i => i.Type != NoscorePocketType.Wear && i.ItemInstance.Item.Effect == effect)
            : null;
    }

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var wearable = (WearableInstance)ctx.Source.ItemInstance;
        wearable.Rare = ctx.Data<RarifyRollState>().NewRare;
        // Re-roll the rarity-driven stat bonuses (defence for armor, damage/concentrate for
        // weapons) to match the new rare tier.
        wearable.SetRarityPoint(rarityPoints, Random);
    }

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx)
    {
        session.Character.InventoryService.RemoveItemAmountFromInventory(1, ctx.Source.ItemInstanceId);
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        yield return RefreshSource(ctx, destroyed: outcome == UpgradeOutcome.Failure);
        if (ctx.Data<RarifyRollState>().RisingAmulet is { } rising)
        {
            yield return ((InventoryItemInstance?)null).GeneratePocketChange(rising.Type.ToPocketType(), rising.Slot);
        }
    }

    protected override int? OutcomeEffect(UpgradeOutcome outcome) => outcome switch
    {
        UpgradeOutcome.Success => 3005,
        UpgradeOutcome.ProtectedSave => 3004,
        _ => null,
    };

    // Success message 147 "Gamble successful! Rarity level: %d" carries the new rare as a
    // long arg so the client renders the actual outcome rather than a generic confirmation.
    protected override SayiPacket? BuildSay(ClientSession session, UpgradeContext ctx, UpgradeOutcome outcome,
        Game18NConstString message)
    {
        if (ctx.Data<RarifyRollState>().RisingAmulet is not null)
        {
            return base.BuildSay(session, ctx, outcome, Game18NConstString.RarityLevelIncreased);
        }

        var say = base.BuildSay(session, ctx, outcome,
            SavedByAmulet(ctx, outcome) ? Game18NConstString.ItemSurvivedWithProtection : message);
        if (say is not null && outcome == UpgradeOutcome.Success)
        {
            say.ArgumentType = IntegerArgument;
            say.Game18NArguments.Add((long)ctx.Data<RarifyRollState>().NewRare);
        }

        return say;
    }

    protected override MsgiPacket BuildMsgi(UpgradeContext ctx, UpgradeOutcome outcome,
        Game18NConstString message)
    {
        if (SavedByAmulet(ctx, outcome))
        {
            return base.BuildMsgi(ctx, outcome, Game18NConstString.ItemSurvivedWithProtection);
        }

        if (ctx.Data<RarifyRollState>().RisingAmulet is not null)
        {
            return base.BuildMsgi(ctx, outcome, Game18NConstString.RarityLevelIncreased);
        }

        if (outcome != UpgradeOutcome.Success)
        {
            return base.BuildMsgi(ctx, outcome, message);
        }
        var newRare = ctx.Data<RarifyRollState>().NewRare;
        return new MsgiPacket
        {
            Type = MessageType.Default,
            Message = message,
            Game18NArguments = new Game18NArguments(1) { (long)newRare },
        };
    }

    private sealed class RarifyRollState
    {
        public RarifyRollState(sbyte originalRare, InventoryItemInstance? amulet, InventoryItemInstance? risingAmulet)
        {
            OriginalRare = originalRare;
            Amulet = amulet;
            RisingAmulet = risingAmulet;
        }

        public sbyte OriginalRare { get; }

        public InventoryItemInstance? Amulet { get; }

        public InventoryItemInstance? RisingAmulet { get; }

        public bool AmuletExpired { get; set; }

        public sbyte NewRare { get; set; }
    }
}
