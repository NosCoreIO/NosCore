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

// Equipment upgrade flow: increase a wearable's Upgrade level by +1 (cap +10), consuming
// cellon (vnum 1014) and gold. Two variants share this base:
//  - Unprotected: failure decrements Upgrade by 1 (floor 0)
//  - Protected:   failure leaves Upgrade unchanged but charges more material
//
// Concrete subclasses set Kind and Protected; everything else is shared.
public abstract class EquipmentUpgradeOperationBase(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : UpgradeOperation(random, localizer)
{
    public const short CellonVNum = 1014;
    public const byte MaxUpgradeLevel = 10;

    // Index by current Upgrade (0..9). Numbers from the legacy server config; tune via
    // the arrays below + EquipmentUpgradeOperationTests fixtures.
    private static readonly double[] SuccessRateByLevel =
        { 1.00, 1.00, 0.90, 0.85, 0.70, 0.50, 0.30, 0.20, 0.10, 0.05 };
    private static readonly long[] GoldCostByLevel =
        { 500, 1500, 3000, 12000, 24000, 80000, 100000, 200000, 600000, 1000000 };
    private static readonly short[] CellonCostByLevel =
        { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    // Protected variants charge double the cellon (no item-degradation safety has a price).
    protected abstract bool IsProtected { get; }

    protected override Game18NConstString SuccessMessage => Game18NConstString.UpgradeSuccessful;

    protected override Game18NConstString FailureMessage =>
        IsProtected ? Game18NConstString.UpgradeFailedButProtected : Game18NConstString.UpgradeFailed;

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        var source = session.Character.InventoryService
            .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
        if (source?.ItemInstance is not WearableInstance wearable || wearable.Upgrade >= MaxUpgradeLevel)
        {
            return null;
        }

        int level = wearable.Upgrade;
        var cellonCost = (short)(CellonCostByLevel[level] * (IsProtected ? 2 : 1));
        return new UpgradeContext(
            Source: source,
            Target: null,
            GoldCost: GoldCostByLevel[level],
            MaterialCosts: new[] { new MaterialCost(CellonVNum, cellonCost) },
            ExtraData: level);
    }

    protected override double GetSuccessRate(UpgradeContext ctx) =>
        SuccessRateByLevel[(int)ctx.ExtraData!];

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var wearable = (WearableInstance)ctx.Source.ItemInstance!;
        wearable.Upgrade = (byte)(wearable.Upgrade + 1);
    }

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx)
    {
        if (IsProtected)
        {
            return;
        }
        var wearable = (WearableInstance)ctx.Source.ItemInstance!;
        if (wearable.Upgrade > 0)
        {
            wearable.Upgrade = (byte)(wearable.Upgrade - 1);
        }
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, bool succeeded)
    {
        yield return ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot);
    }
}
