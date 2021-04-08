//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using Serilog;

namespace NosCore.Core.MessageQueue
{
    [Authorize]
    public class PubSubHub : Hub, IPubSubHub
    {
        private readonly ILogger _logger;
        private readonly MasterClientList _masterClientList;
        private readonly ConcurrentDictionary<Guid, IMessage> _messages = new();

        public PubSubHub(ILogger logger, MasterClientList masterClientList)
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

        public Task BindAsync(Channel data, CancellationToken stoppingToken)
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

            return Task.CompletedTask;
        }

        public Task<List<IMessage>> ReceiveMessagesAsync(int maxNumberOfMessages = 10, TimeSpan? visibilityTimeout = null)
        {
            var messages = _messages.Values.Where(m=>m.VisibilityTimeout > DateTime.Now).Take(maxNumberOfMessages).ToList();
            foreach(var message in messages)
            {
                message.VisibilityTimeout = DateTime.Now.Add(visibilityTimeout ?? TimeSpan.FromSeconds(5));
            }
            return Task.FromResult(messages);
        }

        public Task DeleteMessageAsync(Guid messageId)
        {
            _messages.TryRemove(messageId, out _);
            return Task.CompletedTask;
        }

        public Task SendMessageAsync(IMessage message)
        {
            _messages.TryAdd(message.Id, message);
            return Task.CompletedTask;
        }

        public Task UpdateVisibilityTimeoutAsync(Guid messageId, TimeSpan visibilityTimeout)
        {
            if (_messages.ContainsKey(messageId))
            {
                _messages[messageId].VisibilityTimeout = DateTime.Now.Add(visibilityTimeout);
            }
            return Task.CompletedTask;
        }
    }
}
