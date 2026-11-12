//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Data.Enumerations.Map;
using System;
using System.Linq;

namespace NosCore.GameObject.Services.MapInstanceAccessService
{
    public class MapInstanceAccessorService(IMapInstanceRegistry mapInstanceRegistry) : IMapInstanceAccessorService
    {
        public MapInstance? GetMapInstance(Guid id) => mapInstanceRegistry.GetById(id);

        public MapInstance? GetBaseMapById(short mapId) => mapInstanceRegistry.GetBaseMapById(mapId);

        public MapInstance? GetArenaById(short mapId) => mapInstanceRegistry.GetAll()
            .FirstOrDefault(s => s.Map.MapId == mapId && s.MapInstanceType == MapInstanceType.ArenaInstance);
    }
}
