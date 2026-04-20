//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using NosCore.GameObject.Infastructure;
using NosCore.PathFinder.Interfaces;
using NosCore.PathFinder.Pathfinder;

namespace NosCore.GameObject.Services.PathfindingService;

// Lazy per-map cache. The pathfinder only reads the grid, so cached instances are
// shareable across AI ticks without locking — concurrent FindPath calls against JPS are
// safe because the algorithm keeps per-call state, not per-instance state.
public sealed class PathfindingService(IHeuristic heuristic) : IPathfindingService, ISingletonService
{
    private readonly ConcurrentDictionary<short, IPathfinder> _byMapId = new();

    public IPathfinder ForMap(Map.Map map)
        => _byMapId.GetOrAdd(map.MapId, _ => new JumpPointSearchPathfinder(map, heuristic));
}
