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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Polly;
using Polly.Retry;
using Serilog;

namespace NosCore.GameObject.InterChannelCommunication.Hubs;

public abstract class BaseHubClient : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ILogger _logger;
    private bool _isStarted;

    private static readonly AsyncRetryPolicy RetryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            3,
            attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            (exception, timeSpan, retryCount, _) =>
            {
                Log.Warning(exception, "Hub call failed. Retry {RetryCount} in {TimeSpan}", retryCount, timeSpan);
            });

    protected BaseHubClient(HubConnectionFactory hubConnectionFactory, string hubName, ILogger logger)
    {
        _logger = logger;
        _hubConnection = hubConnectionFactory.Create(hubName);

        _hubConnection.Reconnecting += error =>
        {
            _logger.Warning("Hub {HubName} reconnecting...", hubName);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.Information("Hub {HubName} reconnected", hubName);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += error =>
        {
            _isStarted = false;
            _logger.Warning("Hub {HubName} connection closed", hubName);
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
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            await EnsureConnectedAsync();
            return await _hubConnection.InvokeCoreAsync<T>(methodName, args);
        });
    }

    protected async Task InvokeAsync(string methodName, params object?[] args)
    {
        await RetryPolicy.ExecuteAsync(async () =>
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
