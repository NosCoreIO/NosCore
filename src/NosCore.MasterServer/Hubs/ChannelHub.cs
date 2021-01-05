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
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.MasterServer.Hubs
{
    public class ChannelHub : Hub, IChannelHub
    {
         private readonly ILogger _logger;
        private readonly IOptions<WebApiConfiguration> _apiConfiguration;

        public ChannelHub(ILogger logger, IOptions<WebApiConfiguration> apiConfiguration)
        {
            _apiConfiguration = apiConfiguration;
            _logger = logger;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var data = MasterClientListSingleton.Instance.Channels.ContainsKey(Context.ConnectionId) ? MasterClientListSingleton.Instance.Channels[Context.ConnectionId] : null;
            if (data != null)
            {
                _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHANNEL_CONNECTION_LOST),
                    data.Id.ToString(CultureInfo.CurrentCulture),
                    data.Name);
                MasterClientListSingleton.Instance.Channels.Remove(Context.ConnectionId, out _);
                MasterClientListSingleton.Instance.ConnectedAccounts.Remove(Context.ConnectionId, out _);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, data.Type.ToString());
            }

            await base.OnDisconnectedAsync(exception);
        }

        public Task<List<ChannelInfo>> GetChannels()
        {
            return Task.FromResult(MasterClientListSingleton.Instance.Channels.Values.ToList());
        }

        public async Task Subscribe(Channel data)
        {
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
            MasterClientListSingleton.Instance.Channels.TryAdd(Context.ConnectionId, serv);
            await Clients.Clients(Context.ConnectionId).SendAsync($"{nameof(SubscribedEvent)}Received", new SubscribedEvent());
            MasterClientListSingleton.Instance.ConnectedAccounts.TryAdd(Context.ConnectionId, new ConcurrentDictionary<string, ConnectedAccount>());
            await Groups.AddToGroupAsync(Context.ConnectionId, data.ClientType.ToString());
        }

        public Task RegisterAccount(ConnectedAccount account)
        {
            if (MasterClientListSingleton.Instance.ConnectedAccounts[Context.ConnectionId].ContainsKey(account.Name))
            {
                MasterClientListSingleton.Instance.ConnectedAccounts[Context.ConnectionId][account.Name] = account;
            }
            else
            {
                MasterClientListSingleton.Instance.ConnectedAccounts[Context.ConnectionId].TryAdd(account.Name, account);
            }
            return Task.CompletedTask;
        }

        public Task UnregisterAccount(string accountName)
        {
            MasterClientListSingleton.Instance.ConnectedAccounts[Context.ConnectionId].TryRemove(accountName, out _);
            return Task.CompletedTask;
        }

        public Task BroadcastEvent(Event<IEvent> channelEvent)
        {
            if (channelEvent.ChannelIds.Any())
            {
                var channels = MasterClientListSingleton.Instance.Channels.Where(o => channelEvent.ChannelIds.Contains(o.Value.Id)).ToList();
                if (channels.Any())
                {
                    Clients.Clients(channels.Select(o => o.Key)).SendAsync("EventReceived", channelEvent.Content);
                }
            }
            else
            {
                Clients.Group(ServerType.WorldServer.ToString()).SendAsync($"{channelEvent.Content.GetType().Name}Received", channelEvent.Content);
            }

            return Task.CompletedTask;
        }

        public Task<List<ConnectedAccount>> GetConnectedAccountsAsync()
        {
            return Task.FromResult(MasterClientListSingleton.Instance.ConnectedAccounts.Values.SelectMany(o => o.Values).ToList());
        }
    }
}
