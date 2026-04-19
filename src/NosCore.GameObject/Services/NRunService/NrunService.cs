//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.NRunService
{
    public class NrunService(
            IEnumerable<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>> handlers)
        : INrunService
    {
        private readonly List<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>>
            _handlers = Enumerable.ToList<IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>>(handlers);

        public async Task NRunLaunchAsync(ClientSession clientSession, Tuple<IAliveEntity, NrunPacket> data)
        {
            using var handlersRequest = new Subject<RequestData<Tuple<IAliveEntity, NrunPacket>>>();
            var taskList = new List<Task>();
            var subscriptions = new List<IDisposable>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(data))
                {
                    subscriptions.Add(handlersRequest.Select(request =>
                    {
                        var task = handler.ExecuteAsync(request);
                        taskList.Add(task);
                        return task;
                    }).Subscribe());
                }
            });
            handlersRequest.OnNext(new RequestData<Tuple<IAliveEntity, NrunPacket>>(clientSession, data));
            try
            {
                await Task.WhenAll(taskList).ConfigureAwait(false);
            }
            finally
            {
                foreach (var sub in subscriptions)
                {
                    sub.Dispose();
                }
            }
        }
    }
}
