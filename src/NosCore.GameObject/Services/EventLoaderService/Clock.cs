//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.EventLoaderService
{
    public class Clock(EventLoaderService<Clock, Instant, ITimedEventHandler> eventRunnerService, IClock clock)
        : IRequestableEntity<Instant>
    {
        private readonly IEventLoaderService<Clock, Instant> _eventRunnerService = eventRunnerService;
        public Instant LastTick = clock.GetCurrentInstant();

        public async Task Run(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                LastTick = clock.GetCurrentInstant();
                _eventRunnerService.LoadHandlers(this);
                Requests[typeof(ITimedEventHandler)].OnNext(new RequestData<Instant>(clock.GetCurrentInstant()));
                await Task.Delay(100, stoppingToken);
            }
        }

        public List<Task> HandlerTasks { get; set; } = new();

        public Dictionary<Type, Subject<RequestData<Instant>>> Requests { get; set; } = new()
        {
            [typeof(ITimedEventHandler)] = new Subject<RequestData<Instant>>()
        };
    }
}
