//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace NosCore.GameObject.Services.EventLoaderService
{
    public class EventLoaderService<T1, T2, TEventType>
        (IEnumerable<IEventHandler<T1, T2>> handlers) : IEventLoaderService<T1, T2>
    where T1 : IRequestableEntity<T2>
    where TEventType : IEventHandler
    {
        private readonly List<IEventHandler<T1, T2>> _handlers = Enumerable.ToList<IEventHandler<T1, T2>>(handlers);

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
