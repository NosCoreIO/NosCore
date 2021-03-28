using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core.HubInterfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.GameObject.HubClients.BlacklistHubClient
{
    public class BlacklistHubClient : IBlacklistHubClient
    {
        private readonly ILogger _logger;
        private readonly HubConnection _hubConnection;

        public BlacklistHubClient(IOptions<WebApiConfiguration> worldConfiguration, ILogger logger)
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

        public Task<List<CharacterRelationStatus>> GetBlackListsAsync(long characterVisualId)
        {
            throw new NotImplementedException();
        }

        public Task<LanguageKey> AddToBlacklistAsync(BlacklistRequest blacklistRequest)
        {
            throw new NotImplementedException();
        }

        public Task DeleteFromBlacklistAsync(Guid characterRelationId)
        {
            throw new NotImplementedException();
        }
    }
}
