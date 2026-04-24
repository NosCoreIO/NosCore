//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs;

public abstract class BaseHubClient : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ILogger _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private bool _isStarted;

    protected BaseHubClient(HubConnectionFactory hubConnectionFactory, string hubName, ILogger logger)
    {
        _logger = logger;
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (exception, timeSpan, retryCount, _) =>
                {
                    _logger.LogWarning(exception, "Hub call failed. Retry {RetryCount} in {TimeSpan}", retryCount, timeSpan);
                });
        _hubConnection = hubConnectionFactory.Create(hubName);

        _hubConnection.Reconnecting += error =>
        {
            _logger.LogWarning("Hub {HubName} reconnecting...", hubName);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("Hub {HubName} reconnected", hubName);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += error =>
        {
            _isStarted = false;
            _logger.LogWarning("Hub {HubName} connection closed", hubName);
            return Task.CompletedTask;
        };
    }

    protected async Task EnsureConnectedAsync()
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            return;
        }

        await _connectionLock.WaitAsync();
        try
        {
            if (!_isStarted || _hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
                _isStarted = true;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    protected async Task<T> InvokeAsync<T>(string methodName, params object?[] args)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await EnsureConnectedAsync();
            return await _hubConnection.InvokeCoreAsync<T>(methodName, args);
        });
    }

    protected async Task InvokeAsync(string methodName, params object?[] args)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            await EnsureConnectedAsync();
            await _hubConnection.InvokeCoreAsync(methodName, args);
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                await _hubConnection.StopAsync();
                _isStarted = false;
            }
        }
        finally
        {
            _connectionLock.Release();
        }

        await _hubConnection.DisposeAsync();
        _connectionLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
