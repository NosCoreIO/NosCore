using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Json.Patch;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.HubInterfaces;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.HubClients.ChannelHubClient.Events;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.GameObject.HubClients.ChannelHubClient
{
    public class ChannelHubClient : IChannelHubClient
    {
        private readonly ILogger _logger;
        private readonly HubConnection _hubConnection;

        public ChannelHubClient(IOptions<WebApiConfiguration> worldConfiguration, ILogger logger)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{worldConfiguration.Value}/{nameof(IChannelHub).TrimStart('I')}")
                .Build();
            _logger = logger;
            _hubConnection.On<KickEvent>($"{nameof(KickEvent)}Received", OnEventReceived);
            _hubConnection.On<MaintenanceEvent>($"{nameof(MaintenanceEvent)}Received", OnEventReceived);
            _hubConnection.On<SubscribedEvent>($"{nameof(SubscribedEvent)}Received", OnEventReceived);
        }

        private void OnEventReceived(SubscribedEvent obj)
        {
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.REGISTRED_ON_MASTER));
        }

        private void OnEventReceived(KickEvent kickEvent)
        {

        }
        private void OnEventReceived(MaintenanceEvent maintenanceEvent)
        {

        }

        public Task Subscribe(Channel data)
        {
            return _hubConnection.SendAsync(nameof(IChannelHub.Subscribe), data);
        }

        public Task<List<ChannelInfo>> GetChannels()
        {
            return _hubConnection.InvokeAsync<List<ChannelInfo>>(nameof(IChannelHub.GetChannels));
        }

        public Task RegisterAccount(ConnectedAccount account)
        {
            return _hubConnection.SendAsync(nameof(IChannelHub.RegisterAccount), account);
        }

        public Task UnregisterAccount(string accountName)
        {
            return _hubConnection.SendAsync(nameof(IChannelHub.UnregisterAccount), accountName);
        }

        public Task BroadcastEvent(Event<IEvent> channelEvent)
        {
            return _hubConnection.SendAsync(nameof(IChannelHub.BroadcastEvent), channelEvent);
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            return _hubConnection.StartAsync(stoppingToken);
        }

        public Task<List<ConnectedAccount>> GetConnectedAccountsAsync()
        {
            return _hubConnection.InvokeAsync<List<ConnectedAccount>>(nameof(IChannelHub.GetConnectedAccountsAsync));
        }

        public async Task<ConnectedAccount?> GetCharacterAsync(long? characterId, string? characterName)
        {
            return (await GetConnectedAccountsAsync()).FirstOrDefault(o =>
                o.ConnectedCharacter?.Id == characterId || o.ConnectedCharacter?.Name == characterName);
        }
    }
}
