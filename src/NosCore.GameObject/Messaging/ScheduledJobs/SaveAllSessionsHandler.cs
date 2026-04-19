//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Messaging.ScheduledJobs
{
    [UsedImplicitly]
    public sealed class SaveAllSessionsHandler(
        ILogger<SaveAllSessionsHandler> logger,
        ISaveService saveService,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage,
        ISessionRegistry sessionRegistry)
    {
        [UsedImplicitly]
        public async Task Handle(SaveAllSessionsMessage _)
        {
            logger.LogInformation(logLanguage[LogLanguageKey.SAVING_ALL]);
            await Task.WhenAll(sessionRegistry.GetSessions().Select(saveService.SaveAsync));
        }
    }
}
