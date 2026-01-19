//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Interfaces;
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

        public Task NRunLaunchAsync(ClientSession clientSession, Tuple<IAliveEntity, NrunPacket> data)
        {
            var handlersRequest = new Subject<RequestData<Tuple<IAliveEntity, NrunPacket>>>();
            var taskList = new List<Task>();
            _handlers.ForEach(handler =>
            {
                if (handler.Condition(data))
                {
                    handlersRequest.Select(request =>
                    {
                        var task = handler.ExecuteAsync(request);
                        taskList.Add(task);
                        return task;
                    }).Subscribe();
                }
            });
            handlersRequest.OnNext(new RequestData<Tuple<IAliveEntity, NrunPacket>>(clientSession, data));
            return Task.WhenAll(taskList);
        }
    }
}
