//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ItemGenerationService;
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
    ISpUpgradeService spUpgradeService,
    IItemCatalog itemCatalog)
    : SpecialistCardOperation(random, localizer)
{
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

    protected override UpgradeContext? TryPrepareContext(ClientSession session, UpgradePacket packet)
    {
        var source = LoadCard(session, packet);
        if (source?.ItemInstance is not SpecialistInstance card || !Accepts(card))
        {
            return null;
        }

        return new UpgradeContext(
            Source: source,
            Target: null,
            GoldCost: GoldCost(card),
            MaterialCosts: MaterialCosts(card));
    }

    // Whether this window takes the card at all.
    protected virtual bool Accepts(SpecialistInstance card) => true;

    protected virtual long GoldCost(SpecialistInstance card) => spUpgradeService.GetGoldCost(card.Upgrade);

    protected virtual IReadOnlyList<MaterialCost> MaterialCosts(SpecialistInstance card)
    {
        var upgrade = card.Upgrade;
        var materials = new List<MaterialCost>
        {
            new(VNum(MaterialType.AngelsFeather), spUpgradeService.GetFeatherCost(upgrade)),
            new(VNum(MaterialType.FullMoonCrystal), spUpgradeService.GetFullMoonCost(upgrade)),
        };
        AddIfNeeded(materials, VNum(Soul(card)), spUpgradeService.GetSoulCost(upgrade));
        AddIfNeeded(materials, VNum(MaterialType.DragonGem), spUpgradeService.GetDragonGemCost(upgrade));
        if (card.Item.Morph is >= FirstDimensionMorph and <= LastDimensionMorph)
        {
            AddIfNeeded(materials, VNum(MaterialType.DimensiolithShard), spUpgradeService.GetShardCost(upgrade));
        }
        if (IsProtected)
        {
            materials.Add(new MaterialCost(VNum(Scroll(upgrade)), 1));
        }

        return materials;
    }

    protected override UpgradeOutcome DetermineOutcome(double roll, UpgradeContext ctx)
    {
        var upgrade = ((SpecialistInstance)ctx.Source.ItemInstance).Upgrade;
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
        var card = (SpecialistInstance)ctx.Source.ItemInstance;
        card.Upgrade = (byte)(card.Upgrade + 1);
    }

    protected override void ApplyFailure(ClientSession session, UpgradeContext ctx) { }

    protected override void ApplyDestroyed(UpgradeContext ctx)
    {
        ctx.Source.ItemInstance.Rare = DestroyedSoulRare;
    }

    protected override async Task EmitOutcomeEffectsAsync(ClientSession session, UpgradeContext ctx,
        UpgradeOutcome outcome, List<IPacket> playerPackets)
    {
        if (IsProtected)
        {
            await BroadcastEffectAsync(session, 3004);
        }

        if (outcome == UpgradeOutcome.Success)
        {
            await BroadcastEffectAsync(session, 3005);
        }
    }

    protected override Game18NConstString? Rejection(ClientSession session, SpecialistInstance card)
    {
        if (card.Rare == DestroyedSoulRare)
        {
            return Game18NConstString.CantUseBecauseSoulDestroyed;
        }

        if (!Accepts(card))
        {
            return Game18NConstString.CardCannotBeUpgraded;
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

        return MissingMaterial(session, card);
    }

    protected virtual Game18NConstString? MissingMaterial(ClientSession session, SpecialistInstance card)
    {
        var upgrade = card.Upgrade;
        var inventory = session.Character.InventoryService;
        if (inventory.CountItem(VNum(MaterialType.AngelsFeather)) < spUpgradeService.GetFeatherCost(upgrade))
        {
            return Game18NConstString.NotEnoughAngelFeathers;
        }

        if (inventory.CountItem(VNum(MaterialType.FullMoonCrystal)) < spUpgradeService.GetFullMoonCost(upgrade))
        {
            return Game18NConstString.NotEnoughFullMoon;
        }

        if (inventory.CountItem(VNum(Soul(card))) < spUpgradeService.GetSoulCost(upgrade))
        {
            return Soul(card) switch
            {
                MaterialType.GreenSoul => Game18NConstString.NotEnoughGreenSouls,
                MaterialType.RedSoul => Game18NConstString.NotEnoughRedSouls,
                MaterialType.BlueSoul => Game18NConstString.NotEnoughBlueSouls,
                _ => Game18NConstString.NotEnoughIngredients,
            };
        }

        if (IsProtected && inventory.CountItem(VNum(Scroll(upgrade))) < 1)
        {
            return Game18NConstString.NotEnoughIngredients;
        }

        return null;
    }

    private short VNum(MaterialType material) => ItemCatalog.GetMaterialVNum(material);

    protected IItemCatalog ItemCatalog { get; } = itemCatalog;

    private static MaterialType Soul(SpecialistInstance card)
    {
        var dragon = card.Item.Morph > LastSoulMorph;
        return card.Upgrade switch
        {
            < 5 => dragon ? MaterialType.DragonSkin : MaterialType.GreenSoul,
            < 10 => dragon ? MaterialType.DragonBlood : MaterialType.RedSoul,
            _ => dragon ? MaterialType.DragonHeart : MaterialType.BlueSoul,
        };
    }

    private static void AddIfNeeded(List<MaterialCost> materials, short vnum, short amount)
    {
        if (amount > 0)
        {
            materials.Add(new MaterialCost(vnum, amount, OnSuccessOnly: true));
        }
    }

    private static MaterialType Scroll(byte upgrade) =>
        upgrade < 10 ? MaterialType.LowerSpProtectionScroll : MaterialType.HigherSpProtectionScroll;
}
