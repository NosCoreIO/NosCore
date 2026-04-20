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

// Equipment upgrade flow modeled on OpenNos's WearableInstance.UpgradeItem (lines 667-897).
//
// 3-way roll per OpenNos:
//   - rnd < upfix[Upgrade]                 → Fixed: item becomes IsFixed=true (locked)
//   - rnd < upfix + upfail                 → Failure: item destroyed (or saved by scroll)
//   - else                                 → Success: Upgrade += 1
//
// Two cost tiers indexed by source.Upgrade:
//   - Rare < 8: standard tables
//   - Rare >= 8: scaled-up tables (the "perfect rarity" tier)
//
// Materials per attempt:
//   - Cellon (vnum 1014)
//   - Gem: vnum 1015 if Upgrade<5, else 1016 (the "full" gem)
//   - Protected variants additionally consume one Magic Pearl Scroll (vnum 1218); the
//     scroll absorbs a Failure roll and turns it into a Fixed-style no-op (item preserved).
//
// Caps:
//   - Upgrade >= 10 rejected upfront.
//   - IsFixed items rejected upfront.
public abstract class EquipmentUpgradeOperationBase(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : UpgradeOperation(random, localizer)
{
    public const short CellonVNum = 1014;
    public const short SmallGemVNum = 1015;
    public const short FullGemVNum = 1016;
    public const short NormalScrollVNum = 1218;
    public const byte MaxUpgradeLevel = 10;
    private const byte HighRarityThreshold = 8;

    // OpenNos tables (rare < 8). Indexed by source Upgrade (0..9).
    // upfix and upfail are "out of 100"; success rate = 100 - upfix - upfail.
    private static readonly short[] LowRareUpfix    = { 0,    0,    10,   15,    20,    20,    20,     20,      15,      14 };
    private static readonly short[] LowRareUpfail   = { 0,    0,    0,    5,     20,    40,    60,     70,      80,      85 };
    private static readonly long[]  LowRareGold     = { 500,  1500, 3000, 10000, 30000, 80000, 150000, 400000,  700000,  1000000 };
    private static readonly short[] LowRareCellon   = { 20,   50,   80,   120,   160,   220,   280,    380,     480,     600 };
    private static readonly short[] LowRareGem      = { 1,    1,    2,    2,     3,     1,     1,      2,       2,       3 };

    // OpenNos tables (rare >= 8). Same shape, scaled-up costs.
    private static readonly short[] HighRareUpfix   = { 50,   40,   70,   65,    80,    90,    95,     97,      98,      99 };
    private static readonly short[] HighRareUpfail  = { 50,   40,   60,   50,    60,    70,    75,     77,      83,      89 };
    private static readonly long[]  HighRareGold    = { 5000, 15000, 30000, 100000, 300000, 800000, 1500000, 4000000, 7000000, 10000000 };
    private static readonly short[] HighRareCellon  = { 40,   100,  160,  240,   320,   440,   560,    760,     960,     1200 };
    private static readonly short[] HighRareGem     = { 2,    2,    4,    4,     6,     2,     2,      4,       4,       6 };

    protected abstract bool IsProtected { get; }

    protected override Game18NConstString SuccessMessage => Game18NConstString.UpgradeSuccessful;

    protected override Game18NConstString FailureMessage =>
        IsProtected ? Game18NConstString.UpgradeFailedButProtected : Game18NConstString.UpgradeFailed;

    // Fixed outcome leaves the item unchanged (just locked); reuse the failure message so
    // players see "upgrade failed" framing.
    protected override Game18NConstString FixedMessage => Game18NConstString.UpgradeFailed;

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        var source = session.Character.InventoryService
            .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
        if (source?.ItemInstance is not WearableInstance wearable)
        {
            return null;
        }

        if (wearable.Upgrade >= MaxUpgradeLevel || wearable.IsFixed == true)
        {
            return null;
        }

        var level = (int)wearable.Upgrade;
        var isHighRare = wearable.Rare >= HighRarityThreshold;

        var goldCost = (isHighRare ? HighRareGold : LowRareGold)[level];
        var cellonCost = (isHighRare ? HighRareCellon : LowRareCellon)[level];
        var gemCost = (isHighRare ? HighRareGem : LowRareGem)[level];
        var gemVNum = level < 5 ? SmallGemVNum : FullGemVNum;

        var materials = new List<MaterialCost>
        {
            new(CellonVNum, cellonCost),
            new(gemVNum, gemCost),
        };
        if (IsProtected)
        {
            materials.Add(new MaterialCost(NormalScrollVNum, 1));
        }

        return new UpgradeContext(
            Source: source,
            Target: null,
            GoldCost: goldCost,
            MaterialCosts: materials,
            ExtraData: new EquipmentUpgradeRollData(level, isHighRare));
    }

    // 3-way roll per OpenNos: Fixed band first, then Failure band, else Success.
    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx)
    {
        var data = (EquipmentUpgradeRollData)ctx.ExtraData!;
        var upfix = (data.IsHighRare ? HighRareUpfix : LowRareUpfix)[data.Level] / 100.0;
        var upfail = (data.IsHighRare ? HighRareUpfail : LowRareUpfail)[data.Level] / 100.0;

        if (roll < upfix)
        {
            return UpgradeOutcome.Fixed;
        }
        if (roll < upfix + upfail)
        {
            // Protected variant turns Failure into a no-op (scroll absorbs the destruction).
            // The scroll itself is still consumed via the MaterialCosts path.
            return IsProtected ? UpgradeOutcome.Fixed : UpgradeOutcome.Failure;
        }
        return UpgradeOutcome.Success;
    }

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var wearable = (WearableInstance)ctx.Source.ItemInstance!;
        wearable.Upgrade = (byte)(wearable.Upgrade + 1);
    }

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx)
    {
        // Unprotected failure destroys the item entirely (matches OpenNos line 875).
        session.Character.InventoryService.RemoveItemAmountFromInventory(1, ctx.Source.ItemInstanceId);
    }

    protected override void ApplyFixed(ClientSession session, UpgradeContext ctx)
    {
        var wearable = (WearableInstance)ctx.Source.ItemInstance!;
        wearable.IsFixed = true;
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        if (outcome == UpgradeOutcome.Failure)
        {
            yield return ((InventoryItemInstance?)null).GeneratePocketChange(
                (PocketType)ctx.Source.Type, ctx.Source.Slot);
        }
        else
        {
            yield return ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot);
        }
    }

    private sealed record EquipmentUpgradeRollData(int Level, bool IsHighRare);
}
