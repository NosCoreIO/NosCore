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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.EventLoaderService.Handlers;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using Serilog;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NosCore.Core.HubInterfaces;
using Polly;
using Microsoft.AspNetCore.SignalR.Client;
using NosCore.Core;
using NosCore.GameObject.HubClients.ChannelHubClient;

namespace NosCore.WorldServer
{
    public class WorldServer : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly NetworkManager _networkManager;
        private readonly IOptions<WorldConfiguration> _worldConfiguration;
        private readonly IMapInstanceGeneratorService _mapInstanceGeneratorService;
        private readonly Clock _clock;
        private readonly Channel _channel;
        private readonly IChannelHubClient _channelHubClient;

        public WorldServer(IOptions<WorldConfiguration> worldConfiguration, NetworkManager networkManager, Clock clock, ILogger logger, IMapInstanceGeneratorService mapInstanceGeneratorService,IChannelHubClient channelHubClient, Channel channel)
        {
            _worldConfiguration = worldConfiguration;
            _networkManager = networkManager;
            _logger = logger;
            _mapInstanceGeneratorService = mapInstanceGeneratorService;
            _clock = clock;
            _channel = channel;
            _channelHubClient = channelHubClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _mapInstanceGeneratorService.InitializeAsync().ConfigureAwait(false);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                var eventSaveAll = new SaveAll(_logger);
                _ = eventSaveAll.ExecuteAsync();
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHANNEL_WILL_EXIT));
                Thread.Sleep(30000);
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title += $@" - Port : {_worldConfiguration.Value.Port} - WebApi : {_worldConfiguration.Value.WebApi}";
            }

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LISTENING_PORT),
                _worldConfiguration.Value.Port);
            await Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (_, __, timeSpan) =>
                        _logger.Error(
                            LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_RETRY),
                            timeSpan.TotalSeconds)
                ).ExecuteAsync(() => _channelHubClient.StartAsync(stoppingToken));
            await _channelHubClient.Subscribe(_channel);
            await Task.WhenAny(_clock.Run(stoppingToken), _networkManager.RunServerAsync()).ConfigureAwait(false);
        }
    }
}