using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using NosCore.Core;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventRunnerService;

namespace NosCore.GameObject.Event
{
    public class Clock : IRequestableEntity<DateTime>
    {
        private readonly IEventLoaderService<Clock, DateTime> _eventRunnerService;
        public DateTime LastTick;
        public Clock(EventLoaderService<Clock, DateTime, ITimedEventHandler> eventRunnerService)
        {
            _eventRunnerService = eventRunnerService;
            LastTick = SystemTime.Now();
        }

        public async Task Run(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                LastTick = SystemTime.Now();
                _eventRunnerService.LoadHandlers(this);
                Requests[typeof(ITimedEventHandler)]!.OnNext(new RequestData<DateTime>(SystemTime.Now()));
                await Task.Delay(1000, stoppingToken);
            }
        }

        public List<Task> HandlerTasks { get; set; } = new List<Task>();

        public Dictionary<Type, Subject<RequestData<DateTime>>> Requests { get; set; } = new Dictionary<Type, Subject<RequestData<DateTime>>>
        {
            [typeof(ITimedEventHandler)] = new Subject<RequestData<DateTime>>()
        };
    }
}
