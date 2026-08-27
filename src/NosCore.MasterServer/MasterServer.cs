//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosCore.Core;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.MasterServer
{
    public class MasterServer(IOptions<MasterConfiguration> masterConfiguration, ILogger<MasterServer> logger, 
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : BackgroundService
    {
        private readonly MasterConfiguration _masterConfiguration = masterConfiguration.Value;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation(logLanguage[LogLanguageKey.SUCCESSFULLY_LOADED]);
            ConsoleTitle.Append($@" - WebApi : {_masterConfiguration.WebApi}");

            return Task.CompletedTask;
        }
    }
}
