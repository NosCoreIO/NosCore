//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Buff
{
    // Cards the server applies on its own when a situation arises, rather than through a skill or an item.
    public enum CardTrigger : byte
    {
        None = 0,
        SpouseInGroup = 1,
        ArenaRevival = 2
    }
}
