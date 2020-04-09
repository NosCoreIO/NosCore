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
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Providers.MapInstanceProvider.Handlers;

namespace NosCore.GameObject.Providers.MapInstanceProvider
{
    public interface IMapInstanceProvider
    {
        Guid GetBaseMapInstanceIdByMapId(short mapId);
        MapInstance? GetMapInstance(Guid id);
        MapInstance? GetBaseMapById(short mapId);
        void AddMapInstance(MapInstance mapInstance);
        Task InitializeAsync();
        void RemoveMap(Guid guid);

        MapInstance CreateMapInstance(Map.Map map, Guid guid, bool shopAllowed, MapInstanceType normalInstance,
            List<IMapInstanceEventHandler> mapInstanceEventHandler);
    }
}