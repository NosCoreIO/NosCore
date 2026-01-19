//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Map;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.MapInstanceGenerationService
{
    public class MapInstanceRegistry : IMapInstanceRegistry
    {
        private ConcurrentDictionary<Guid, MapInstance> _mapInstances = new();

        public MapInstance? GetById(Guid mapInstanceId) =>
            _mapInstances.TryGetValue(mapInstanceId, out var instance) ? instance : null;

        public MapInstance? GetBaseMapById(short mapId) =>
            _mapInstances.Values.FirstOrDefault(s => s.Map.MapId == mapId && s.MapInstanceType == MapInstanceType.BaseMapInstance);

        public IEnumerable<MapInstance> GetAll() => _mapInstances.Values;

        public void Register(Guid mapInstanceId, MapInstance mapInstance) =>
            _mapInstances.TryAdd(mapInstanceId, mapInstance);

        public bool Unregister(Guid mapInstanceId, out MapInstance? mapInstance) =>
            _mapInstances.TryRemove(mapInstanceId, out mapInstance);

        public void SetAll(IDictionary<Guid, MapInstance> mapInstances) =>
            _mapInstances = new ConcurrentDictionary<Guid, MapInstance>(mapInstances);
    }
}
