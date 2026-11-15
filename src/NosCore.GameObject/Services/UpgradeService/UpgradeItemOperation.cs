//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Shops;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Data.Enumerations.Items;
using JetBrains.Annotations;
using NosCore.Algorithm.EquipmentUpgradeService;
using NosCore.Core.I18N;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.UpgradeService;

[UsedImplicitly]
public sealed class UpgradeItemOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        IEquipmentUpgradeService equipmentUpgradeService, IItemCatalog itemCatalog)
    : EquipmentUpgradeOperationBase(random, localizer, equipmentUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeItem;

    public override NpcService? Service => NpcService.Upgrade;

    protected override bool IsProtected => false;
}

[UsedImplicitly]
public sealed class UpgradeItemProtectedOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        IEquipmentUpgradeService equipmentUpgradeService, IItemCatalog itemCatalog)
    : EquipmentUpgradeOperationBase(random, localizer, equipmentUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeItemProtected;

    public override NpcService? Service => NpcService.Upgrade;

    protected override bool IsProtected => true;
}

[UsedImplicitly]
public sealed class UpgradeItemGoldScrollOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        IEquipmentUpgradeService equipmentUpgradeService, IItemCatalog itemCatalog)
    : EquipmentUpgradeOperationBase(random, localizer, equipmentUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeItemGoldScroll;

    public override NpcService? Service => NpcService.Upgrade;

    protected override bool IsProtected => true;

    protected override MaterialType ProtectionScroll => MaterialType.GoldenEquipmentProtectionScroll;

    protected override int GoldAndCellaDivisor => 2;
}
