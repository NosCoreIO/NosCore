using NosCore.Shared.Enumerations.Map;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace NosCore.GameObject
{
    public static class MapInstanceListExtension
    {
        public static Guid GetBaseMapInstanceIdByMapId(this ConcurrentDictionary<Guid, MapInstance> mapInstances, short mapId)
        {
            return mapInstances.FirstOrDefault(s =>
                s.Value?.Map.MapId == mapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Key;
        }

        public static MapInstance GetMapInstance(this ConcurrentDictionary<Guid, MapInstance> mapInstances, Guid id)
        {
            return mapInstances.ContainsKey(id) ? mapInstances[id] : null;
        }
    }
}
