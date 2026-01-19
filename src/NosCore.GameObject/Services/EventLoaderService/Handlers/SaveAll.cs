//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Shared.I18N;
using System;
using System.Linq;
using System.Threading.Tasks;

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
