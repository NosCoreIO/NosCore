using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Shared.Configuration;
using Polly;
using Serilog;

namespace NosCore.Core.MessageQueue
{
    public class PubSubHubClient : IPubSubHub
    {
        private readonly HubConnection _hubConnection;

        public PubSubHubClient(IOptions<WebApiConfiguration> configuration)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{configuration.Value}/{nameof(PubSubHub)}")
                .Build();
        }

        public async Task BindAsync(Channel data, CancellationToken stoppingToken)
        {
            await _hubConnection.StartAsync(stoppingToken);
            try
            {
                await _hubConnection.InvokeAsync(nameof(BindAsync), data, stoppingToken, stoppingToken);
            }
            finally
            {
                await _hubConnection.StopAsync(stoppingToken);
            }
        }

        public Task<List<IMessage>> ReceiveMessagesAsync(int maxNumberOfMessages = 10, TimeSpan? visibilityTimeout = null)
        {
            return _hubConnection.InvokeAsync<List<IMessage>>(nameof(ReceiveMessagesAsync), maxNumberOfMessages, visibilityTimeout);
        }

        public Task DeleteMessageAsync(Guid messageId)
        {
            return _hubConnection.InvokeAsync(nameof(DeleteMessageAsync), messageId);
        }

        public Task SendMessageAsync(IMessage message)
        {
            return _hubConnection.InvokeAsync(nameof(ReceiveMessagesAsync), message);
        }

        public Task UpdateVisibilityTimeoutAsync(Guid messageId, TimeSpan visibilityTimeout)
        {
            return _hubConnection.InvokeAsync(nameof(UpdateVisibilityTimeoutAsync), messageId, visibilityTimeout);
        }
    }
}
