//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.RarityPointService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Data.Enumerations.Items;
using JetBrains.Annotations;
using NosCore.Core.I18N;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.UpgradeService;

[UsedImplicitly]
public sealed class RarifyOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        IItemCatalog itemCatalog, IRarityPointService rarityPoints)
    : RarifyOperationBase(random, localizer, itemCatalog, rarityPoints)
{
    public override UpgradePacketType Kind => UpgradePacketType.RarifyItem;

    protected override bool IsProtected => false;
}

[UsedImplicitly]
public sealed class RarifyProtectedOperation(IRandomNumberSource random, IGameLanguageLocalizer localizer,
        IItemCatalog itemCatalog, IRarityPointService rarityPoints)
    : RarifyOperationBase(random, localizer, itemCatalog, rarityPoints)
{
    public override UpgradePacketType Kind => UpgradePacketType.RarifyItemProtected;

    protected override bool IsProtected => true;
}
