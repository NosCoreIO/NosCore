//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ItemGenerationService;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using JetBrains.Annotations;
using NosCore.Algorithm.SpUpgradeService;
using NosCore.Core.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.UpgradeService;

// An event card is upgraded in a window its own scroll opens, and the scroll is the only thing
// the attempt costs. The odds are the ones of a plain card upgrade.
public abstract class EventSpUpgradeOperationBase(
    IRandomNumberSource random,
    IGameLanguageLocalizer localizer,
    ISpUpgradeService spUpgradeService,
    IItemCatalog itemCatalog)
    : SpUpgradeOperationBase(random, localizer, spUpgradeService, itemCatalog), IItemOpenedUpgradeOperation
{
    private short ScrollVNum => ItemCatalog.GetItemsWithEffect(ItemEffectType.EventSpUpgradeScroll)
        .FirstOrDefault(i => i.EffectValue == (int)Kind)?.VNum ?? 0;

    public bool IsOpenedBy(ItemDto item) => IsFor(item, ItemEffectType.EventSpUpgradeScroll);

    public override NpcDialogWindow Window => NpcDialogWindow.UpgradeScroll;

    protected override bool IsProtected => false;

    protected override bool Accepts(SpecialistInstance card) => IsFor(card.Item, ItemEffectType.EventSpecialist);

    private bool IsFor(ItemDto item, ItemEffectType effect) => item.Effect == effect && item.EffectValue == (int)Kind;

    protected override long GoldCost(SpecialistInstance card) => 0;

    protected override IReadOnlyList<MaterialCost> MaterialCosts(SpecialistInstance card) =>
        new[] { new MaterialCost(ScrollVNum, 1) };

    protected override Game18NConstString? MissingMaterial(ClientSession session, SpecialistInstance card) =>
        session.Character.InventoryService.CountItem(ScrollVNum) < 1
            ? Game18NConstString.NotEnoughIngredients
            : null;
}

[UsedImplicitly]
public sealed class ChickenSpUpgradeOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        ISpUpgradeService spUpgradeService, IItemCatalog itemCatalog)
    : EventSpUpgradeOperationBase(random, localizer, spUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeSpChiken;
}

[UsedImplicitly]
public sealed class PyjamaSpUpgradeOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        ISpUpgradeService spUpgradeService, IItemCatalog itemCatalog)
    : EventSpUpgradeOperationBase(random, localizer, spUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeSpPyjama;
}

[UsedImplicitly]
public sealed class PirateSpUpgradeOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        ISpUpgradeService spUpgradeService, IItemCatalog itemCatalog)
    : EventSpUpgradeOperationBase(random, localizer, spUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeSpPirate;
}
