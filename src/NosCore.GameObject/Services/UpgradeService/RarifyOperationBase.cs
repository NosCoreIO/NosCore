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

// Rarify flow: increase a wearable's Rare stat (-2..+8) by +1, consuming a Red Stellar
// Magic Stone (vnum 1024) and gold. Two variants share this base:
//  - Unprotected: failure resets Rare to 0 (item is now generic).
//  - Protected:   failure leaves Rare unchanged but uses a Blue Stellar instead.
public abstract class RarifyOperationBase(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : UpgradeOperation(random, localizer)
{
    public const short RedStellarVNum = 1024;
    public const short BlueStellarVNum = 1025;
    public const sbyte MaxRarity = 8;

    // Index by current Rare (0..7). Same baked-in tuning as the legacy server config.
    private static readonly double[] SuccessRateByRarity =
        { 0.95, 0.85, 0.75, 0.50, 0.30, 0.18, 0.10, 0.05 };
    private static readonly long[] GoldCostByRarity =
        { 5000, 12000, 25000, 50000, 100000, 200000, 400000, 800000 };

    protected abstract bool IsProtected { get; }

    protected override Game18NConstString SuccessMessage => Game18NConstString.RarityLevelIncreased;

    protected override Game18NConstString FailureMessage =>
        IsProtected ? Game18NConstString.RarityUnchangedProtectionScroll : Game18NConstString.RarityOnZero;

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        var source = session.Character.InventoryService
            .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
        if (source?.ItemInstance is not WearableInstance wearable
            || wearable.Rare < 0
            || wearable.Rare >= MaxRarity)
        {
            return null;
        }

        var index = (int)wearable.Rare;
        var stellar = IsProtected ? BlueStellarVNum : RedStellarVNum;
        return new UpgradeContext(
            Source: source,
            Target: null,
            GoldCost: GoldCostByRarity[index],
            MaterialCosts: new[] { new MaterialCost(stellar, 1) },
            ExtraData: index);
    }

    protected override double GetSuccessRate(UpgradeContext ctx) =>
        SuccessRateByRarity[(int)ctx.ExtraData!];

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var wearable = (WearableInstance)ctx.Source.ItemInstance!;
        wearable.Rare = (sbyte)(wearable.Rare + 1);
    }

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx)
    {
        if (IsProtected)
        {
            return;
        }
        var wearable = (WearableInstance)ctx.Source.ItemInstance!;
        wearable.Rare = 0;
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, bool succeeded)
    {
        yield return ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot);
    }
}
