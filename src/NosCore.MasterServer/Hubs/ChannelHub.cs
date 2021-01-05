using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using NosCore.Core;
using NosCore.Core.HubInterfaces;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.MasterServer.Hubs
{
    public class ChannelHub : Hub<IChannelHub>, IChannelHub
    {
        private readonly ConcurrentDictionary<string, ChannelInfo> _channels = new ConcurrentDictionary<string, ChannelInfo>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConnectedAccount>> _connectedAccounts = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConnectedAccount>>();
        private readonly ILogger _logger;
        private readonly IOptions<WebApiConfiguration> _apiConfiguration;

        public ChannelHub(ILogger logger, IOptions<WebApiConfiguration> apiConfiguration)
        {
            _apiConfiguration = apiConfiguration;
            _logger = logger;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var data = _channels.ContainsKey(Context.ConnectionId) ? _channels[Context.ConnectionId] : null;
            if (data != null)
            {
                _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHANNEL_CONNECTION_LOST),
                    data.Id.ToString(CultureInfo.CurrentCulture),
                    data.Name);
                _channels.Remove(Context.ConnectionId, out _);
                _connectedAccounts.Remove(Context.ConnectionId, out _);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Channels");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public Task<List<ChannelInfo>> GetChannels()
        {
            return Task.FromResult(_channels.Values.ToList());
        }

        public async Task Subscribe(Channel data)
        {
            if (data.MasterCommunication!.Password != _apiConfiguration.Value.Password)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTHENTICATED_ERROR));
                return;
            }

            var id = ++MasterClientListSingleton.Instance.ConnectionCounter;
            _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTHENTICATED_SUCCESS),
                id.ToString(CultureInfo.CurrentCulture),
                data.ClientName);

            var serv = new ChannelInfo
            {
                Name = data.ClientName,
                Host = data.Host,
                Port = data.Port,
                DisplayPort = (ushort?)data.DisplayPort,
                DisplayHost = data.DisplayHost,
                IsMaintenance = data.StartInMaintenance,
                Id = id,
                ConnectedAccountLimit = data.ConnectedAccountLimit,
                WebApi = data.WebApi,
                Type = data.ClientType,
            };
            _channels.TryAdd(Context.ConnectionId, serv);
            _connectedAccounts.TryAdd(Context.ConnectionId, new ConcurrentDictionary<string, ConnectedAccount>());
            await Groups.AddToGroupAsync(Context.ConnectionId, "Channels");
        }

        public Task RegisterAccount(ConnectedAccount account)
        {
            if (_connectedAccounts[Context.ConnectionId].ContainsKey(account.Name))
            {
                _connectedAccounts[Context.ConnectionId][account.Name] = account;
            }
            else
            {
                _connectedAccounts[Context.ConnectionId].TryAdd(account.Name, account);
            }
            return Task.CompletedTask;
        }

        public Task UnregisterAccount(string accountName)
        {
            _connectedAccounts[Context.ConnectionId].TryRemove(accountName, out _);
            return Task.CompletedTask;
        }
    }
}
