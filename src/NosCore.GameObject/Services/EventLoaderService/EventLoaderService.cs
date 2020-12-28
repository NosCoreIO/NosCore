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

using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace NosCore.GameObject.Services.EventLoaderService
{
    public class EventLoaderService<T1, T2, TEventType> : IEventLoaderService<T1, T2>
    where T1 : IRequestableEntity<T2>
    where TEventType : IEventHandler
    {
        private readonly List<IEventHandler<T1, T2>> _handlers;

        public EventLoaderService(IEnumerable<IEventHandler<T1, T2>> handlers)
        {
            _handlers = handlers.ToList();
        }

        public void LoadHandlers(T1 item)
        {
            var handlersRequest = new Subject<RequestData<T2>>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(item))
                {
                    handlersRequest.Select(request =>
                    {
                        var task = handler.ExecuteAsync(request);
                        item.HandlerTasks.Add(task);
                        return task;
                    }).Subscribe();
                }
            });
            item.Requests[typeof(TEventType)] = handlersRequest;
        }
    }
}