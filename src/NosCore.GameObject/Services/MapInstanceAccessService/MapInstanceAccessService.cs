//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.MapInstanceGenerationService;
using System;

namespace NosCore.GameObject.Services.MapInstanceAccessService
{
    public class MapInstanceAccessorService(IMapInstanceRegistry mapInstanceRegistry) : IMapInstanceAccessorService
    {
        public MapInstance? GetMapInstance(Guid id) => mapInstanceRegistry.GetById(id);

        public MapInstance? GetBaseMapById(short mapId) => mapInstanceRegistry.GetBaseMapById(mapId);
    }
}
