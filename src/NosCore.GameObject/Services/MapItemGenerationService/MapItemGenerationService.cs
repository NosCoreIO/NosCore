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

using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Drops;
using System;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public class MapItemGenerationService : IMapItemGenerationService
    {
        private readonly IEventLoaderService<MapItem, Tuple<MapItem, GetPacket>> _runner;

        public MapItemGenerationService(EventLoaderService<MapItem, Tuple<MapItem, GetPacket>, IGetMapItemEventHandler> runner)
        {
            _runner = runner;
        }

        public MapItem Create(MapInstance mapInstance, IItemInstance itemInstance, short positionX, short positionY)
        {
            var mapItem = new MapItem
            {
                MapInstance = mapInstance,
                ItemInstance = itemInstance,
                PositionX = positionX,
                PositionY = positionY
            };
            _runner.LoadHandlers(mapItem);
            return mapItem;
        }
    }
}