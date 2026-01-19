//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.MapInstanceGenerationService
{
    public interface IMapInstanceRegistry
    {
        MapInstance? GetById(Guid mapInstanceId);
        MapInstance? GetBaseMapById(short mapId);
        IEnumerable<MapInstance> GetAll();
        void Register(Guid mapInstanceId, MapInstance mapInstance);
        bool Unregister(Guid mapInstanceId, out MapInstance? mapInstance);
        void SetAll(IDictionary<Guid, MapInstance> mapInstances);
    }
}
