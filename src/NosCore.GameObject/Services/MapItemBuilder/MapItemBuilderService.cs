//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Linq;
using System.Reactive.Subjects;
using Mapster;
using NosCore.Core.Extensions;
using NosCore.Data;
using NosCore.GameObject.Handling;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemBuilder.Handling;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject.Services.MapBuilder
{
    public class MapItemBuilderService : IMapItemBuilderService
    {
        private readonly List<IHandler<MapItem, Tuple<MapItem, GetPacket>>> _handlers;
        public MapItemBuilderService(IEnumerable<IHandler<MapItem, Tuple<MapItem, GetPacket>>> handlers)
        {
            _handlers = handlers.ToList();
        }

        private void LoadHandlers(MapItem item)
        {
            var handlersRequest = new Subject<RequestData<Tuple<MapItem, GetPacket>>>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(item))
                {
                    var itemHandler = handler.GetType().CreateInstance<IHandler<MapItem, Tuple<MapItem, GetPacket>>>();
                    handlersRequest.Subscribe(itemHandler.Execute);
                }
            });
            item.Requests = handlersRequest;
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
            LoadHandlers(mapItem);
            return mapItem;
        }
    }
}