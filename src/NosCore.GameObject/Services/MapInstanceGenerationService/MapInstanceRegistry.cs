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
