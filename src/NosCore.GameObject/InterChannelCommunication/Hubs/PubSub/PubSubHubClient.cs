using Microsoft.AspNetCore.SignalR.Client;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.PubSub
{
    public class PubSubHubClient : IPubSubHubClient, IAsyncDisposable
    {
        private readonly HubConnection _hubConnection;
        private readonly ILogger _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private bool _isStarted;

        public event Action<IMessage>? OnMessageReceived;

        public bool IsConnected => _hubConnection.State == HubConnectionState.Connected;

        public PubSubHubClient(HubConnectionFactory hubConnectionFactory, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _logger = logger;
            _logLanguage = logLanguage;
            _hubConnection = hubConnectionFactory.Create(nameof(PubSubHub));

            _hubConnection.On<IMessage>("ReceiveMessage", message =>
            {
                OnMessageReceived?.Invoke(message);
            });

            _hubConnection.Reconnecting += error =>
            {
                _logger.Warning(_logLanguage[LogLanguageKey.PUBSUB_RECONNECTING]);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                _logger.Information(_logLanguage[LogLanguageKey.PUBSUB_RECONNECTED]);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                _logger.Warning(_logLanguage[LogLanguageKey.PUBSUB_CONNECTION_CLOSED]);
                return Task.CompletedTask;
            };
        }

        public async Task StartAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (_isStarted && _hubConnection.State == HubConnectionState.Connected)
                {
                    return;
                }

                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                    _isStarted = true;
                    _logger.Information(_logLanguage[LogLanguageKey.PUBSUB_CONNECTION_STARTED]);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task StopAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (_hubConnection.State != HubConnectionState.Disconnected)
                {
                    await _hubConnection.StopAsync();
                    _isStarted = false;
                    _logger.Information(_logLanguage[LogLanguageKey.PUBSUB_CONNECTION_STOPPED]);
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private async Task EnsureConnectedAsync()
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await StartAsync();
            }
        }

        public async Task<bool> SendMessageAsync(IMessage message)
        {
            await EnsureConnectedAsync();
            return await _hubConnection.InvokeAsync<bool>(nameof(SendMessageAsync), message);
        }

        public async Task<bool> SendMessagesAsync(List<IMessage> messages)
        {
            await EnsureConnectedAsync();
            return await _hubConnection.InvokeAsync<bool>(nameof(SendMessagesAsync), messages);
        }

        public async Task<List<Subscriber>> GetSubscribersAsync()
        {
            await EnsureConnectedAsync();
            return await _hubConnection.InvokeAsync<List<Subscriber>>(nameof(GetSubscribersAsync));
        }

        public async Task<bool> SubscribeAsync(Subscriber subscriber)
        {
            await EnsureConnectedAsync();
            return await _hubConnection.InvokeAsync<bool>(nameof(SubscribeAsync), subscriber);
        }

        public async Task<bool> UnsubscribeAsync(long id)
        {
            await EnsureConnectedAsync();
            return await _hubConnection.InvokeAsync<bool>(nameof(UnsubscribeAsync), id);
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            await _hubConnection.DisposeAsync();
            _connectionLock.Dispose();
        }
    }
}