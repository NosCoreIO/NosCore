//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.RaidService
{
    // What an rd packet asks for.
    public enum RdRequest : short
    {
        Join = 1,
        Leave = 2,
        Kick = 3,
        Disband = 4
    }
}
