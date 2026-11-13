//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Map
{
    // Which monsters join in when one of a group is fighting.
    public enum GroupAttackType : byte
    {
        None = 0,
        SameRace = 1,
        SameIcon = 5
    }
}
