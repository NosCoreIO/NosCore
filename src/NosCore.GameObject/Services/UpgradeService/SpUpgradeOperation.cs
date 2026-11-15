//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Data.Enumerations.Items;
using JetBrains.Annotations;
using NosCore.Algorithm.SpUpgradeService;
using NosCore.Core.I18N;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.UpgradeService;

[UsedImplicitly]
public sealed class SpUpgradeOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        ISpUpgradeService spUpgradeService, IItemCatalog itemCatalog)
    : SpUpgradeOperationBase(random, localizer, spUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeSpNoProtection;

    public override NpcService? Service => NpcService.SpecialistCardUpgrade;

    protected override bool IsProtected => false;
}

[UsedImplicitly]
public sealed class SpUpgradeProtectedOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        ISpUpgradeService spUpgradeService, IItemCatalog itemCatalog)
    : SpUpgradeOperationBase(random, localizer, spUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeSpProtected;

    public override NpcService? Service => NpcService.SpecialistCardUpgrade;

    protected override bool IsProtected => true;
}

[UsedImplicitly]
public sealed class SpUpgradeProtected2Operation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        ISpUpgradeService spUpgradeService, IItemCatalog itemCatalog)
    : SpUpgradeOperationBase(random, localizer, spUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeSpProtected2;

    public override NpcService? Service => NpcService.SpecialistCardUpgrade;

    protected override bool IsProtected => true;
}
