//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Data.Enumerations.Items;
using JetBrains.Annotations;
using NosCore.Algorithm.EquipmentUpgradeService;
using NosCore.Core.I18N;
using NosCore.Packets.Enumerations;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Services.UpgradeService;

[UsedImplicitly]
public sealed class UpgradeItemOperation(IRandomProvider random, IGameLanguageLocalizer localizer,
        IEquipmentUpgradeService equipmentUpgradeService, IItemCatalog itemCatalog)
    : EquipmentUpgradeOperationBase(random, localizer, equipmentUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeItem;

    protected override bool IsProtected => false;
}

[UsedImplicitly]
public sealed class UpgradeItemProtectedOperation(IRandomProvider random, IGameLanguageLocalizer localizer,
        IEquipmentUpgradeService equipmentUpgradeService, IItemCatalog itemCatalog)
    : EquipmentUpgradeOperationBase(random, localizer, equipmentUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeItemProtected;

    protected override bool IsProtected => true;
}

[UsedImplicitly]
public sealed class UpgradeItemGoldScrollOperation(IRandomProvider random, IGameLanguageLocalizer localizer,
        IEquipmentUpgradeService equipmentUpgradeService, IItemCatalog itemCatalog)
    : EquipmentUpgradeOperationBase(random, localizer, equipmentUpgradeService, itemCatalog)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeItemGoldScroll;

    protected override bool IsProtected => true;

    protected override MaterialType ProtectionScroll => MaterialType.GoldenEquipmentProtectionScroll;

    protected override int GoldAndCellaDivisor => 2;
}
