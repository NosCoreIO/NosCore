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
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;

namespace NosCore.GameObject.Providers.MapItemProvider
{
    public class MapItemProvider : IMapItemProvider
    {
        private readonly List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>> _handlers;

        public MapItemProvider(IEnumerable<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>> handlers)
        {
            _handlers = handlers.ToList();
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

        private void LoadHandlers(MapItem item)
        {
            var handlersRequest = new Subject<RequestData<Tuple<MapItem, GetPacket>>>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(item))
                {
                    handlersRequest.Subscribe(async o => await Observable.FromAsync(async () =>
                    {
                        await handler.Execute(o);
                    }));
                }
            });
            item.Requests = handlersRequest;
        }
    }
}