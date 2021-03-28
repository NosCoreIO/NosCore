using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core.HubInterfaces;
using NosCore.Data.WebApi;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.GameObject.HubClients.PacketHubClient
{
    public class PacketHubClient : IPacketHubClient
    {
        private readonly ILogger _logger;
        private readonly HubConnection _hubConnection;

        public PacketHubClient(IOptions<WebApiConfiguration> worldConfiguration, ILogger logger)
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

        public Task BroadcastPacketAsync(PostedPacket packet, int channelId)
        {
            throw new System.NotImplementedException();
        }

        public Task BroadcastPacketAsync(PostedPacket packet)
        {
            throw new System.NotImplementedException();
        }

        public Task BroadcastPacketsAsync(List<PostedPacket> packets)
        {
            throw new System.NotImplementedException();
        }

        public Task BroadcastPacketsAsync(List<PostedPacket> packets, int channelId)
        {
            throw new System.NotImplementedException();
        }
    }
}
