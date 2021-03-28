using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core.HubInterfaces;
using NosCore.Data.WebApi;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.GameObject.HubClients.StatHubClient
{
    public class StatHubClient : IStatHubClient
    {
        private readonly ILogger _logger;
        private readonly HubConnection _hubConnection;

        public StatHubClient(IOptions<WebApiConfiguration> worldConfiguration, ILogger logger)
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

        public Task ChangeStatAsync(StatData data, int channelId)
        {
            throw new System.NotImplementedException();
        }
    }
}
