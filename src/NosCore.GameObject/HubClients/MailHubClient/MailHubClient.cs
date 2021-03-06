using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core.HubInterfaces;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.GameObject.HubClients.MailHubClient
{
    public class MailHubClient : IMailHubClient
    {
        private readonly ILogger _logger;
        private readonly HubConnection _hubConnection;

        public MailHubClient(IOptions<WebApiConfiguration> worldConfiguration, ILogger logger)
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
