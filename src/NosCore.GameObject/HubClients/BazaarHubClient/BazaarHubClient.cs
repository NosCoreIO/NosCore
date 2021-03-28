using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Json.Patch;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core.HubInterfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.GameObject.HubClients.BazaarHubClient
{
    public class BazaarHubClient : IBazaarHubClient
    {
        private readonly ILogger _logger;
        private readonly HubConnection _hubConnection;

        public BazaarHubClient(IOptions<WebApiConfiguration> worldConfiguration, ILogger logger)
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

        public Task<List<BazaarLink>> GetBazaarLinksAsync(int i, int packetIndex, int pagesize, BazaarListType packetTypeFilter,
            byte packetSubTypeFilter, byte packetLevelFilter, byte packetRareFilter, byte packetUpgradeFilter,
            long? sellerFilter)
        {
            throw new System.NotImplementedException();
        }

        public Task<LanguageKey?> AddBazaarAsync(BazaarRequest bazaarRequest)
        {
            throw new System.NotImplementedException();
        }

        public Task<BazaarLink?> GetBazaarLinkAsync(long bazaarId)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> RemoveAsync(long bazaarId, int count, string requestCharacterName)
        {
            throw new System.NotImplementedException();
        }

        public Task<BazaarLink> ModifyAsync(long bazaarId, JsonPatch patchBz)
        {
            throw new System.NotImplementedException();
        }
    }
}
