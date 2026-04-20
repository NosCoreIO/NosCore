//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.PathFinder.Interfaces;

namespace NosCore.GameObject.Services.PathfindingService;

// JumpPointSearchPathfinder is instantiated per IMapGrid, so we cache one instance per
// Map.MapId. Pathfinding is read-only against the grid, which means the cached instance
// is safe to share between concurrent AI ticks.
public interface IPathfindingService
{
    IPathfinder ForMap(Map.Map map);
}
