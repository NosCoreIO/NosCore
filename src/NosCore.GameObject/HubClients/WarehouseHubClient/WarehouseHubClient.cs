using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core.HubInterfaces;
using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.GameObject.HubClients.WarehouseHubClient
{
    public class WarehouseHubClient : IWarehouseHubClient
    {
        private readonly ILogger _logger;
        private readonly HubConnection _hubConnection;

        public WarehouseHubClient(IOptions<WebApiConfiguration> worldConfiguration, ILogger logger)
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

        public Task<List<WarehouseItem>> GetWarehouseItemsAsync(long characterId, WarehouseType warehouse)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> DepositItemAsync(long characterCharacterId, WarehouseType warehouse, IItemInstance itemInstance, short slot)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteWarehouseItemAsync(long characterId, WarehouseType warehouse, short slot)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<WarehouseItem>> MoveWarehouseItemAsync(long characterId, WarehouseType warehouse, short slot, short destinationSlot)
        {
            throw new System.NotImplementedException();
        }
    }
}
