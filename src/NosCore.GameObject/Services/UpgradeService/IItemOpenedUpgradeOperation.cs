//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.GameObject.Services.UpgradeService;

// An upgrade whose window is opened by using an item rather than at an NPC.
public interface IItemOpenedUpgradeOperation : IUpgradeOperation
{
    IReadOnlyCollection<short> OpeningItemVNums { get; }
}
