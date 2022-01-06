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
