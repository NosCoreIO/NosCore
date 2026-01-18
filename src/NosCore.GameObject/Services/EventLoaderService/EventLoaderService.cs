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

using NosCore.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.EventLoaderService
{
    public class EventLoaderService<T1, T2, TEventType>
        (IEnumerable<IEventHandler<T1, T2>> handlers) : IEventLoaderService<T1, T2>
    where TEventType : IEventHandler
    {
        private readonly List<IEventHandler<T1, T2>> _handlers = Enumerable.ToList<IEventHandler<T1, T2>>(handlers);

        public void LoadHandlers(T1 item, Dictionary<Type, Subject<RequestData<T2>>> requests, List<Task> handlerTasks)
        {
            var handlersRequest = new Subject<RequestData<T2>>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(item))
                {
                    handlersRequest.Select(request =>
                    {
                        var task = handler.ExecuteAsync(request);
                        handlerTasks.Add(task);
                        return task;
                    }).Subscribe();
                }
            });
            requests[typeof(TEventType)] = handlersRequest;
        }
    }
}