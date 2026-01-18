//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Enumerations.Map;

namespace NosCore.GameObject.Services.MapInstanceGenerationService
{
    public class MapInstanceRegistry : IMapInstanceRegistry
    {
        private ConcurrentDictionary<Guid, MapInstance> _mapInstances = new();

        public MapInstance? GetMapInstance(Guid id)
        {
            return _mapInstances.TryGetValue(id, out var instance) ? instance : null;
        }

        public MapInstance? GetBaseMapById(short mapId)
        {
            var mapInstance = _mapInstances.FirstOrDefault(s =>
                (s.Value?.Map.MapId == mapId) && (s.Value.MapInstanceType == MapInstanceType.BaseMapInstance));
            return mapInstance.Key == default ? null : mapInstance.Value;
        }

        public bool TryRegister(Guid id, MapInstance mapInstance)
        {
            return _mapInstances.TryAdd(id, mapInstance);
        }

        public bool TryUnregister(Guid id, out MapInstance? mapInstance)
        {
            return _mapInstances.TryRemove(id, out mapInstance);
        }

        public void Initialize(IDictionary<Guid, MapInstance> mapInstances)
        {
            _mapInstances = new ConcurrentDictionary<Guid, MapInstance>(mapInstances);
        }

        public IEnumerable<MapInstance> GetAll()
        {
            return _mapInstances.Values;
        }
    }
}
