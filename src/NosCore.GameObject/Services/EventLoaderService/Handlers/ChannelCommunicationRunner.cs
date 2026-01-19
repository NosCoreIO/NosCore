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
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using System.Collections.Generic;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using Serilog;
using IMessage = NosCore.GameObject.InterChannelCommunication.Messages.IMessage;

namespace NosCore.GameObject.Services.EventLoaderService.Handlers
{
    [UsedImplicitly]
    public class ChannelCommunicationRunner : IHostedService
    {
        private readonly IPubSubHubClient _pubSubHubClient;
        private readonly Dictionary<Type, IChannelCommunicationMessageHandler<IMessage>> _handlers;
        private readonly ILogger _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public ChannelCommunicationRunner(
            IPubSubHubClient pubSubHubClient,
            IEnumerable<IChannelCommunicationMessageHandler<IMessage>> handlers,
            ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _pubSubHubClient = pubSubHubClient;
            _logger = logger;
            _logLanguage = logLanguage;
            _handlers = handlers.ToDictionary(
                x => x.GetType().BaseType?.GetGenericArguments().Single() ?? throw new InvalidOperationException(),
                x => x);

            _pubSubHubClient.OnMessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(IMessage message)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_handlers.TryGetValue(message.GetType(), out var handler))
                    {
                        await handler.Handle(message);
                    }
                    else
                    {
                        _logger.Warning(_logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, _logLanguage[LogLanguageKey.PACKET_HANDLING_ERROR]);
                }
            });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _pubSubHubClient.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _pubSubHubClient.OnMessageReceived -= OnMessageReceived;
            await _pubSubHubClient.StopAsync();
        }
    }
}