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

using JetBrains.Annotations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.EventLoaderService.Handlers
{
    [UsedImplicitly]
    public class SaveAll : ITimedEventHandler
    {
        private readonly ILogger<SaveAll> _logger;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;
        private readonly ISessionRegistry _sessionRegistry;

        public SaveAll(ILogger<SaveAll> logger, IClock clock, ISaveService saveService, ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry)
        {
            _logger = logger;
            _clock = clock;
            _lastRun = _clock.GetCurrentInstant();
            _saveService = saveService;
            _logLanguage = logLanguage;
            _sessionRegistry = sessionRegistry;
        }

        private Instant _lastRun;
        private readonly IClock _clock;
        private readonly ISaveService _saveService;

        public bool Condition(Clock condition) => condition.LastTick.Minus(_lastRun).ToTimeSpan() >= TimeSpan.FromMinutes(5);

        public Task ExecuteAsync() => ExecuteAsync(new RequestData<Instant>(_clock.GetCurrentInstant()));

        public async Task ExecuteAsync(RequestData<Instant> runTime)
        {
            _logger.LogInformation(_logLanguage[LogLanguageKey.SAVING_ALL]);
            await Task.WhenAll(_sessionRegistry.GetCharacters().Select(session => _saveService.SaveAsync(session)));

            _lastRun = runTime.Data;
        }
    }
}