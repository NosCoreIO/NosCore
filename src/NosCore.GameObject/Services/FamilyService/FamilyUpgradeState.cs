//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.FamilyService
{
    /// <summary>The tab a row of the family manager's list sits in.</summary>
    public enum FamilyUpgradeState : byte
    {
        Purchasable = 0,
        Purchased = 1,
        Unavailable = 2
    }
}
