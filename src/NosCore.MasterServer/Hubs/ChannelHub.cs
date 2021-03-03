using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NosCore.Core;
using NosCore.Core.HubInterfaces;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.MasterServer.Hubs
{
    public class ChannelHub : Hub, IChannelHub
    {
        private readonly ILogger _logger;
        private readonly MasterClientList _masterClientList;

        public ChannelHub(ILogger logger, MasterClientList masterClientList)
        {
            _logger = logger;
            _masterClientList = masterClientList;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var data = _masterClientList.Channels.ContainsKey(Context.ConnectionId) ? _masterClientList.Channels[Context.ConnectionId] : null;
            if (data != null)
            {
                _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHANNEL_CONNECTION_LOST),
                    data.Id.ToString(CultureInfo.CurrentCulture),
                    data.Name);
                _masterClientList.Channels.Remove(Context.ConnectionId, out _);
                _masterClientList.ConnectedAccounts.Remove(Context.ConnectionId, out _);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, data.Type.ToString());
            }

            await base.OnDisconnectedAsync(exception);
        }

        public Task<List<ChannelInfo>> GetChannels()
        {
            return Task.FromResult(_masterClientList.Channels.Values.ToList());
        }

        public async Task Subscribe(Channel data)
        {
            var id = ++_masterClientList.ConnectionCounter;
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
            _masterClientList.Channels.TryAdd(Context.ConnectionId, serv);
            await Clients.Clients(Context.ConnectionId).SendAsync($"{nameof(SubscribedEvent)}Received", new SubscribedEvent());
            _masterClientList.ConnectedAccounts.TryAdd(Context.ConnectionId, new ConcurrentDictionary<string, ConnectedAccount>());
            await Groups.AddToGroupAsync(Context.ConnectionId, data.ClientType.ToString());
        }

        public Task RegisterAccount(ConnectedAccount account)
        {
            if (_masterClientList.ConnectedAccounts[Context.ConnectionId].ContainsKey(account.Name))
            {
                _masterClientList.ConnectedAccounts[Context.ConnectionId][account.Name] = account;
            }
            else
            {
                _masterClientList.ConnectedAccounts[Context.ConnectionId].TryAdd(account.Name, account);
            }
            return Task.CompletedTask;
        }

        public Task UnregisterAccount(string accountName)
        {
            _masterClientList.ConnectedAccounts[Context.ConnectionId].TryRemove(accountName, out _);
            return Task.CompletedTask;
        }

        public Task BroadcastEvent(Event<IEvent> channelEvent)
        {
            if (channelEvent.ChannelIds.Any())
            {
                var channels = _masterClientList.Channels.Where(o => channelEvent.ChannelIds.Contains(o.Value.Id)).ToList();
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
            return Task.FromResult(_masterClientList.ConnectedAccounts.Values.SelectMany(o => o.Values).ToList());
        }

        public Task<string?> GetAwaitingConnectionAsync(string? name, string? packetPassword, int clientSessionSessionId)
        {
            if (name == null)
            {
                if (packetPassword == null || packetPassword == "NONE_SESSION_TICKET")
                {
                    return Task.FromResult<string?>(null);
                }
                var sessionGuid = HexStringToString(packetPassword);
                if (!_masterClientList.AuthCodes.ContainsKey(sessionGuid))
                {
                    return Task.FromResult<string?>(null);
                }
                var username = _masterClientList.AuthCodes[sessionGuid];
                return Task.FromResult<string?>(username);
            }
            
            if ((_masterClientList.ReadyForAuth.ContainsKey(name) &&
                (clientSessionSessionId == _masterClientList.ReadyForAuth[name])))
            {
                return Task.FromResult<string?>(name);
            }

            return Task.FromResult<string?>(null);
        }

        private static string HexStringToString(string hexString)
        {
            var bb = Enumerable.Range(0, hexString.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                .ToArray();
            return Encoding.UTF8.GetString(bb);
        }

        public Task SetAwaitingConnectionAsync(long sessionId, string accountName)
        {
            _masterClientList.ReadyForAuth.AddOrUpdate(accountName, sessionId, (key, oldValue) => sessionId);
            return Task.CompletedTask;
        }
    }
}
