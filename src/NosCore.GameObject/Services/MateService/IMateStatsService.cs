//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.MateService;

public interface IMateStatsService
{
    // Sets the experience bar, the HP and MP maxima, and the attack, defence and dodge for the mate's current level.
    void ApplyLevel(Mate mate);
}
