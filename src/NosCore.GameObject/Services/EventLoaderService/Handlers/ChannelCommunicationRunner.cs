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

using JetBrains.Annotations;
using NosCore.GameObject.Networking.ClientSession;
using System;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using System.Collections.Generic;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using IMessage = NosCore.GameObject.InterChannelCommunication.Messages.IMessage;


namespace NosCore.GameObject.Services.EventLoaderService.Handlers
{
    [UsedImplicitly]
    public class ChannelCommunicationRunner : ITimedEventHandler
    {

        public ChannelCommunicationRunner(IClock clock, IPubSubHub pubSubHub, IEnumerable<IChannelCommunicationMessageHandler<IMessage>> handler)
        {
            _clock = clock;
            _lastRun = _clock.GetCurrentInstant();
            _pubSubHub = pubSubHub;
            _handler = handler.ToDictionary(x => x.GetType()
                .BaseType?.GetGenericArguments().Single() ?? throw new InvalidOperationException(), x => x);
        }

        private Instant _lastRun;
        private readonly IClock _clock;
        private readonly IPubSubHub _pubSubHub;
        private readonly Dictionary<Type, IChannelCommunicationMessageHandler<IMessage>> _handler;

        public bool Condition(Clock condition) => condition.LastTick.Minus(_lastRun).ToTimeSpan() >= TimeSpan.FromMilliseconds(100);

        public Task ExecuteAsync() => ExecuteAsync(new RequestData<Instant>(_clock.GetCurrentInstant()));

        public async Task ExecuteAsync(RequestData<Instant> runTime)
        {
            var messages = await _pubSubHub.ReceiveMessagesAsync();
            await Task.WhenAll(messages.Select(message => _handler[message.GetType()].Handle(message)));
            _lastRun = runTime.Data;
        }
    }
}