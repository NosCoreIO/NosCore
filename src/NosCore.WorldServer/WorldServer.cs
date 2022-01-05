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
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.EventLoaderService.Handlers;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using Serilog;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Services.SaveService;
using NosCore.Networking;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer
{
    public class WorldServer : BackgroundService
    {
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly ILogger _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;
        private readonly NetworkManager _networkManager;
        private readonly IOptions<WorldConfiguration> _worldConfiguration;
        private readonly IMapInstanceGeneratorService _mapInstanceGeneratorService;
        private readonly Clock _clock;
        private readonly IClock _nodatimeClock;
        private readonly ISaveService _saveService;

        public WorldServer(IOptions<WorldConfiguration> worldConfiguration, NetworkManager networkManager, Clock clock, ILogger logger, 
            IChannelHttpClient channelHttpClient, IMapInstanceGeneratorService mapInstanceGeneratorService, IClock nodatimeClock, ISaveService saveService, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _worldConfiguration = worldConfiguration;
            _networkManager = networkManager;
            _logger = logger;
            _channelHttpClient = channelHttpClient;
            _mapInstanceGeneratorService = mapInstanceGeneratorService;
            _clock = clock;
            _nodatimeClock = nodatimeClock;
            _saveService = saveService;
            _logLanguage = logLanguage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _mapInstanceGeneratorService.InitializeAsync().ConfigureAwait(false);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                var eventSaveAll = new SaveAll(_logger, _nodatimeClock, _saveService, _logLanguage);
                _ = eventSaveAll.ExecuteAsync();
                _logger.Information(_logLanguage[LogLanguageKey.CHANNEL_WILL_EXIT]);
                Thread.Sleep(30000);
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title += $@" - Port : {_worldConfiguration.Value.Port} - WebApi : {_worldConfiguration.Value.WebApi}";
            }

            await Task.WhenAny(_clock.Run(stoppingToken), _channelHttpClient.ConnectAsync(), _networkManager.RunServerAsync()).ConfigureAwait(false);
        }
    }
}