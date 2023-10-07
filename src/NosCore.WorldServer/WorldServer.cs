﻿//  __  _  __    __   ___ __  ___ ___
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
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.GameObject.Services.SaveService;
using NosCore.Networking;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer
{
    public class WorldServer(IOptions<WorldConfiguration> worldConfiguration, NetworkManager networkManager,
            Clock clock, ILogger<WorldServer> logger,
            IChannelHttpClient channelHttpClient, IMapInstanceGeneratorService mapInstanceGeneratorService,
            IClock nodatimeClock, ISaveService saveService,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, ILogger<SaveAll> saveAllLogger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await mapInstanceGeneratorService.InitializeAsync().ConfigureAwait(false);
            logger.LogInformation(logLanguage[LogLanguageKey.SUCCESSFULLY_LOADED]);
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                var eventSaveAll = new SaveAll(saveAllLogger, nodatimeClock, saveService, logLanguage);
                _ = eventSaveAll.ExecuteAsync();
                logger.LogInformation(logLanguage[LogLanguageKey.CHANNEL_WILL_EXIT], 30);
                Thread.Sleep(30000);
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title += $@" - Port : {worldConfiguration.Value.Port} - WebApi : {worldConfiguration.Value.WebApi}";
            }

            await Task.WhenAny(clock.Run(stoppingToken), channelHttpClient.ConnectAsync(), networkManager.RunServerAsync()).ConfigureAwait(false);
        }
    }
}