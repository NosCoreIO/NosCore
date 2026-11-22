//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Battle
{
    // Monsters the server summons or recognises on its own rather than through a map or a script.
    public enum MonsterRole : byte
    {
        None = 0,
        OnyxDragon = 1,
        FactionGuardian = 2
    }
}
