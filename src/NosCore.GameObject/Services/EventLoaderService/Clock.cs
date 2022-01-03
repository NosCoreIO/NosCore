using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;

namespace NosCore.GameObject.Services.EventLoaderService
{
    public class Clock : IRequestableEntity<Instant>
    {
        private readonly IEventLoaderService<Clock, Instant> _eventRunnerService;
        public Instant LastTick;
        private readonly IClock _clock;

        public Clock(EventLoaderService<Clock, Instant, ITimedEventHandler> eventRunnerService, IClock clock)
        {
            _eventRunnerService = eventRunnerService;
            LastTick = clock.GetCurrentInstant();
            _clock = clock;
        }

        public async Task Run(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                LastTick = _clock.GetCurrentInstant();
                _eventRunnerService.LoadHandlers(this);
                Requests[typeof(ITimedEventHandler)]!.OnNext(new RequestData<Instant>(_clock.GetCurrentInstant()));
                await Task.Delay(1000, stoppingToken);
            }
        }

        public List<Task> HandlerTasks { get; set; } = new();

        public Dictionary<Type, Subject<RequestData<Instant>>> Requests { get; set; } = new()
        {
            [typeof(ITimedEventHandler)] = new Subject<RequestData<Instant>>()
        };
    }
}
