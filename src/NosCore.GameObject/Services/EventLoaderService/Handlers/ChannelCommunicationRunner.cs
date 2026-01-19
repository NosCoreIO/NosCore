//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
