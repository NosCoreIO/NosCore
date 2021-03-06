using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core;
using NosCore.Core.HubInterfaces;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.HubClients.ChannelHubClient.Events;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.GameObject.HubClients.FriendHubClient
{
    public class FriendHubClient : IFriendHubClient
    {
        private readonly ILogger _logger;
        private readonly HubConnection _hubConnection;

        public FriendHubClient(IOptions<WebApiConfiguration> worldConfiguration, ILogger logger)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{worldConfiguration.Value}/{nameof(IChannelHub).TrimStart('I')}")
                .Build();
            _logger = logger;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            return _hubConnection.StartAsync(stoppingToken);
        }
    }
}
