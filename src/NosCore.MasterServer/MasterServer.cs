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
using NodaTime;
using NosCore.Core;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.MasterServer
{
    public class MasterServer(IOptions<MasterConfiguration> masterConfiguration, ILogger logger, IClock clock,
            IIdService<ChannelInfo> channelIdService, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : BackgroundService
    {
        private readonly MasterConfiguration _masterConfiguration = masterConfiguration.Value;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!Debugger.IsAttached)
            {
                Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(_ => channelIdService.Items.Values
                    .Where(s =>
                        (s.LastPing.Plus(Duration.FromSeconds(10)) < clock.GetCurrentInstant()) && (s.WebApi != null)).Select(s => s.Id).ToList()
                    .ForEach(id =>
                    {
                        logger.Warning(logLanguage[LogLanguageKey.CONNECTION_LOST],
                            id.ToString());
                        channelIdService.Items.TryRemove(id, out var _);
                    }));
            }

            logger.Information(logLanguage[LogLanguageKey.SUCCESSFULLY_LOADED]);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title += $@" - WebApi : {_masterConfiguration.WebApi}";
            }

            return Task.CompletedTask;
        }
    }
}