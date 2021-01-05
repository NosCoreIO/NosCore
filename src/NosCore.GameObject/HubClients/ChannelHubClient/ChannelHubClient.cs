using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.HubInterfaces;
using NosCore.Data.WebApi;
using NosCore.Shared.Configuration;

namespace NosCore.GameObject.HubClients.ChannelHubClient
{
    public class ChannelHubClient : IChannelHubClient
    {
        private readonly HubConnection _hubConnection;
        public ChannelHubClient(IOptions<LoginConfiguration> loginConfiguration) :
            this(loginConfiguration.Value.MasterCommunication.ToString())
        {
        }

        public ChannelHubClient(IOptions<WorldConfiguration> worldConfiguration) :
            this(worldConfiguration.Value.MasterCommunication.ToString())
        {
        }

        private ChannelHubClient(string url)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{url}/{nameof(IChannelHub).TrimStart('I')}")
                .Build();
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

        public Task StartAsync(in CancellationToken stoppingToken)
        {
            return _hubConnection.StartAsync(stoppingToken);
        }
    }
}
