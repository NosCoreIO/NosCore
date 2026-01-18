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
using NosCore.Core.Services.IdService;

namespace NosCore.GameObject.Services.MapItemGenerationService
{
    public class MapItemGenerationService(
            EventLoaderService<MapItemRef, Tuple<MapItemRef, GetPacket>, IGetMapItemEventHandler> runner,
            IIdService<MapItemRef> mapItemIdService,
            IMapItemRegistry mapItemRegistry)
        : IMapItemGenerationService
    {
        private readonly IEventLoaderService<MapItemRef, Tuple<MapItemRef, GetPacket>> _runner = runner;

        public MapItemRef Create(MapInstance mapInstance, IItemInstance itemInstance, short positionX, short positionY)
        {
            var visualId = mapItemIdService.GetNextId();
            var entity = mapInstance.EcsWorld.CreateMapItem(visualId, itemInstance.ItemVNum, positionX, positionY, null, itemInstance.Amount);
            var data = mapItemRegistry.GetOrCreate(entity, itemInstance);
            var mapItemRef = new MapItemRef(entity, mapInstance, data);
            _runner.LoadHandlers(mapItemRef, data.Requests, data.HandlerTasks);
            return mapItemRef;
        }
    }
}