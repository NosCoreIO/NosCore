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
using NosCore.Core.I18N;

using NosCore.Data.Enumerations.I18N;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Core;

namespace NosCore.MasterServer
{
    public class MasterServer : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly MasterConfiguration _masterConfiguration;
        private readonly IClock _clock;

        public MasterServer(IOptions<MasterConfiguration> masterConfiguration, ILogger logger, IClock clock)
        {
            _masterConfiguration = masterConfiguration.Value;
            _logger = logger;
            _clock = clock;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!Debugger.IsAttached)
            {
                Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(_ => MasterClientListSingleton.Instance.Channels
                    .Where(s =>
                        (s.LastPing.Plus(Duration.FromSeconds(10)) < _clock.GetCurrentInstant()) && (s.WebApi != null)).Select(s => s.Id).ToList()
                    .ForEach(id =>
                    {
                        _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CONNECTION_LOST),
                            id.ToString());
                        MasterClientListSingleton.Instance.Channels.RemoveAll(s => s.Id == id);
                    }));
            }

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title += $@" - WebApi : {_masterConfiguration.WebApi}";
            }

            return Task.CompletedTask;
        }
    }
}