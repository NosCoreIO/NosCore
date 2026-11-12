//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.MateService;

public interface IMateStatsService
{
    // Sets the experience bar and the HP and MP maxima for the mate's current level.
    void ApplyLevel(Mate mate);
}
