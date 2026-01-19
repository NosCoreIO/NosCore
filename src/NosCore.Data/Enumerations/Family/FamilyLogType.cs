//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;

namespace NosCore.Data.Enumerations.Family
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum FamilyLogType : byte
    {
        DailyMessage = 1,
        RaidWon = 2,
        RainbowBattle = 3,
        FamilyXp = 4,
        FamilyLevelUp = 5,
        LevelUp = 6,
        ItemUpgraded = 7,
        RightChanged = 8,
        AuthorityChanged = 9,
        FamilyManaged = 10,
        UserManaged = 11,
        WareHouseAdded = 12,
        WareHouseRemoved = 13
    }
}
