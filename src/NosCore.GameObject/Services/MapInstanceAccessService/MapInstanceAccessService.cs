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
using System.Linq;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Holders;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Services.MapInstanceAccessService
{
    public class MapInstanceAccessorService : IMapInstanceAccessorService
    {
        private readonly MapInstanceHolder _holder;

        public MapInstanceAccessorService(MapInstanceHolder holder)
        {
            _holder = holder;
        }
        public Guid GetBaseMapInstanceIdByMapId(short mapId)
        {
            return _holder.MapInstances.FirstOrDefault(s =>
                (s.Value?.Map.MapId == mapId) && (s.Value.MapInstanceType == MapInstanceType.BaseMapInstance)).Key;
        }

        public MapInstance? GetMapInstance(Guid id)
        {
            return _holder.MapInstances.ContainsKey(id) ? _holder.MapInstances[id] : null;
        }

        public MapInstance GetBaseMapById(short mapId)
        {
            return _holder.MapInstances.FirstOrDefault(s =>
                (s.Value?.Map.MapId == mapId) && (s.Value.MapInstanceType == MapInstanceType.BaseMapInstance)).Value;
        }
    }
}
