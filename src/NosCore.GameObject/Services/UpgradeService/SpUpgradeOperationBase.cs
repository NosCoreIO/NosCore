//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Algorithm.SpUpgradeService;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.UpgradeService;

// One roll out of 100 against the card's upgrade level: below the destruction rate the soul is
// destroyed, below the failure rate nothing changes, anything else adds one level. Feathers, full
// moons and gold are spent on every attempt; souls only on a success. A protection scroll is
// spent on every attempt and turns a destroyed soul into a plain failure.
public abstract class SpUpgradeOperationBase(
    IRandomNumberSource random,
    IGameLanguageLocalizer localizer,
    ISpUpgradeService spUpgradeService)
    : UpgradeOperation(random, localizer)
{
    public const short FeatherVNum = 2282;
    public const short FullMoonVNum = 1030;
    public const short GreenSoulVNum = 2283;
    public const short RedSoulVNum = 2284;
    public const short BlueSoulVNum = 2285;
    public const short DragonSkinVNum = 2511;
    public const short DragonBloodVNum = 2512;
    public const short DragonHeartVNum = 2513;
    public const short DragonGemVNum = 2630;
    public const short ShardVNum = 2748;
    public const short BlueScrollVNum = 1363;
    public const short RedScrollVNum = 1364;
    public const sbyte DestroyedSoulRare = -2;

    // Cards with a higher morph are upgraded with the dragon materials instead of souls.
    private const short LastSoulMorph = 16;

    // The cards of the other dimensions, which also take shards from +15.
    private const short FirstDimensionMorph = 55;
    private const short LastDimensionMorph = 58;

    protected abstract bool IsProtected { get; }

    protected override Game18NConstString SuccessMessage => Game18NConstString.UpgradeSuccessful;

    protected override Game18NConstString FailureMessage => IsProtected
        ? Game18NConstString.SpecialistUpgradeFailedItemsProtected
        : Game18NConstString.SpecialistUpgradeFailed;

    protected override Game18NConstString ProtectedSaveMessage => Game18NConstString.SpecialistSouldProtected;

    protected override Game18NConstString SayMessageFor(UpgradeOutcome outcome) =>
        outcome == UpgradeOutcome.Destroyed
            ? Game18NConstString.SpecialistSouldDestroyed
            : base.SayMessageFor(outcome);

    protected override IReadOnlyList<IPacket>? TryReject(ClientSession session, UpgradePacket packet)
    {
        if (LoadCard(session, packet)?.ItemInstance is not SpecialistInstance card)
        {
            return null;
        }

        var rejection = Rejection(session, card);
        return rejection is null ? null : new IPacket[]
        {
            new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = rejection.Value,
            },
        };
    }

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        var source = LoadCard(session, packet);
        if (source?.ItemInstance is not SpecialistInstance card)
        {
            return null;
        }

        var upgrade = card.Upgrade;
        var materials = new List<MaterialCost>
        {
            new(FeatherVNum, spUpgradeService.GetFeatherCost(upgrade)),
            new(FullMoonVNum, spUpgradeService.GetFullMoonCost(upgrade)),
        };
        AddIfNeeded(materials, SoulVNum(card), spUpgradeService.GetSoulCost(upgrade));
        AddIfNeeded(materials, DragonGemVNum, spUpgradeService.GetDragonGemCost(upgrade));
        if (card.Item.Morph is >= FirstDimensionMorph and <= LastDimensionMorph)
        {
            AddIfNeeded(materials, ShardVNum, spUpgradeService.GetShardCost(upgrade));
        }
        if (IsProtected)
        {
            materials.Add(new MaterialCost(ScrollVNum(upgrade), 1));
        }

        return new UpgradeContext(
            Source: source,
            Target: null,
            GoldCost: spUpgradeService.GetGoldCost(upgrade),
            MaterialCosts: materials);
    }

    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx)
    {
        var upgrade = ((SpecialistInstance)ctx.Source.ItemInstance!).Upgrade;
        var percent = roll * 100;
        if (percent < spUpgradeService.GetDestructionRate(upgrade))
        {
            return IsProtected ? UpgradeOutcome.ProtectedSave : UpgradeOutcome.Destroyed;
        }

        return percent < 100 - spUpgradeService.GetSuccessRate(upgrade)
            ? UpgradeOutcome.Failure
            : UpgradeOutcome.Success;
    }

    protected override void ApplySuccess(UpgradeContext ctx)
    {
        var card = (SpecialistInstance)ctx.Source.ItemInstance!;
        card.Upgrade = (byte)(card.Upgrade + 1);
    }

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx) { }

    protected override void ApplyDestroyed(UpgradeContext ctx)
    {
        ctx.Source.ItemInstance!.Rare = DestroyedSoulRare;
    }

    protected override async Task EmitOutcomeEffectsAsync(ClientSession session, UpgradeContext ctx,
        UpgradeOutcome outcome, List<IPacket> playerPackets)
    {
        if (IsProtected)
        {
            await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateEff(3004));
        }

        if (outcome == UpgradeOutcome.Success)
        {
            await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateEff(3005));
        }
    }

    protected override IEnumerable<IPacket> BuildPocketRefresh(UpgradeContext ctx, UpgradeOutcome outcome)
    {
        yield return ctx.Source.GeneratePocketChange((PocketType)ctx.Source.Type, ctx.Source.Slot);
    }

    private static InventoryItemInstance? LoadCard(ClientSession session, UpgradePacket packet)
    {
        var source = session.Character.InventoryService
            .LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
        return source?.ItemInstance is SpecialistInstance { Item.EquipmentSlot: EquipmentType.Sp } ? source : null;
    }

    private Game18NConstString? Rejection(ClientSession session, SpecialistInstance card)
    {
        if (card.Rare == DestroyedSoulRare)
        {
            return Game18NConstString.CantUseBecauseSoulDestroyed;
        }

        var upgrade = card.Upgrade;
        if (upgrade >= spUpgradeService.MaxUpgrade)
        {
            return Game18NConstString.MaximumUpgradeReached;
        }

        if (card.SpLevel < spUpgradeService.GetMinimumJobLevel(upgrade))
        {
            return upgrade switch
            {
                < 5 => Game18NConstString.SpecialistCardRequireLevelTwentyOne,
                < 10 => Game18NConstString.SpecialistCardRequireLevelFourtyOne,
                _ => Game18NConstString.SpecialistCardRequireLevelFiftyOne,
            };
        }

        var inventory = session.Character.InventoryService;
        if (inventory.CountItem(FeatherVNum) < spUpgradeService.GetFeatherCost(upgrade))
        {
            return Game18NConstString.NotEnoughAngelFeathers;
        }

        if (inventory.CountItem(FullMoonVNum) < spUpgradeService.GetFullMoonCost(upgrade))
        {
            return Game18NConstString.NotEnoughFullMoon;
        }

        if (inventory.CountItem(SoulVNum(card)) < spUpgradeService.GetSoulCost(upgrade))
        {
            return SoulVNum(card) switch
            {
                GreenSoulVNum => Game18NConstString.NotEnoughGreenSouls,
                RedSoulVNum => Game18NConstString.NotEnoughRedSouls,
                BlueSoulVNum => Game18NConstString.NotEnoughBlueSouls,
                _ => Game18NConstString.NotEnoughIngredients,
            };
        }

        if (IsProtected && inventory.CountItem(ScrollVNum(upgrade)) < 1)
        {
            return Game18NConstString.NotEnoughIngredients;
        }

        return null;
    }

    private static short SoulVNum(SpecialistInstance card)
    {
        var dragon = card.Item.Morph > LastSoulMorph;
        return card.Upgrade switch
        {
            < 5 => dragon ? DragonSkinVNum : GreenSoulVNum,
            < 10 => dragon ? DragonBloodVNum : RedSoulVNum,
            _ => dragon ? DragonHeartVNum : BlueSoulVNum,
        };
    }

    private static void AddIfNeeded(List<MaterialCost> materials, short vnum, short amount)
    {
        if (amount > 0)
        {
            materials.Add(new MaterialCost(vnum, amount, OnSuccessOnly: true));
        }
    }

    private static short ScrollVNum(byte upgrade) => upgrade < 10 ? BlueScrollVNum : RedScrollVNum;
}
