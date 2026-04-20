//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Services.BattleService;

// HP/MP natural regeneration. Called from MapInstance's life loop so every connected
// player on the map ticks together — sitting players regen full-rate, standing
// players half-rate. Matches OpenNos Character timer block.
public interface IRegenerationService
{
    Task TickAsync(MapInstance mapInstance);
}
