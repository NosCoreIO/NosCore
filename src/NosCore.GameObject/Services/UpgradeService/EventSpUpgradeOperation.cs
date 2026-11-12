//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ItemGenerationService;
using System.Collections.Generic;
using System.Linq;
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
    public abstract short ScrollVNum { get; }

    public IReadOnlyCollection<short> OpeningItemVNums => new[] { ScrollVNum };

    protected abstract IReadOnlyCollection<short> CardVNums { get; }

    public override NpcDialogWindow Window => NpcDialogWindow.UpgradeScroll;

    protected override bool IsProtected => false;

    protected override bool Accepts(SpecialistInstance card) => CardVNums.Contains(card.ItemVNum);

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
    public const short ChickenScrollVNum = 5107;
    public const short ChickenCardVNum = 907;

    public override UpgradePacketType Kind => UpgradePacketType.UpgradeSpChiken;

    public override short ScrollVNum => ChickenScrollVNum;

    protected override IReadOnlyCollection<short> CardVNums { get; } = new[] { ChickenCardVNum };
}

[UsedImplicitly]
public sealed class PyjamaSpUpgradeOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        ISpUpgradeService spUpgradeService, IItemCatalog itemCatalog)
    : EventSpUpgradeOperationBase(random, localizer, spUpgradeService, itemCatalog)
{
    public const short PyjamaScrollVNum = 5207;
    public const short PyjamaCardVNum = 900;

    public override UpgradePacketType Kind => UpgradePacketType.UpgradeSpPyjama;

    public override short ScrollVNum => PyjamaScrollVNum;

    protected override IReadOnlyCollection<short> CardVNums { get; } = new[] { PyjamaCardVNum };
}

[UsedImplicitly]
public sealed class PirateSpUpgradeOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        ISpUpgradeService spUpgradeService, IItemCatalog itemCatalog)
    : EventSpUpgradeOperationBase(random, localizer, spUpgradeService, itemCatalog)
{
    public const short PirateScrollVNum = 5519;
    public const short PirateCardVNum = 4099;
    public const short EventPirateCardVNum = 4100;

    public override UpgradePacketType Kind => UpgradePacketType.UpgradeSpPirate;

    public override short ScrollVNum => PirateScrollVNum;

    protected override IReadOnlyCollection<short> CardVNums { get; } = new[] { PirateCardVNum, EventPirateCardVNum };
}
