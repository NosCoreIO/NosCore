//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.UpgradeService;

[UsedImplicitly]
public sealed class UpgradeItemOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : EquipmentUpgradeOperationBase(random, localizer)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeItem;

    protected override bool IsProtected => false;
}

[UsedImplicitly]
public sealed class UpgradeItemProtectedOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer)
    : EquipmentUpgradeOperationBase(random, localizer)
{
    public override UpgradePacketType Kind => UpgradePacketType.UpgradeItemProtected;

    protected override bool IsProtected => true;
}
