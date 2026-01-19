//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.MasterServer
{
    public class MasterServer(IOptions<MasterConfiguration> masterConfiguration, ILogger logger, 
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : BackgroundService
    {
        private readonly MasterConfiguration _masterConfiguration = masterConfiguration.Value;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.Information(logLanguage[LogLanguageKey.SUCCESSFULLY_LOADED]);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title += $@" - WebApi : {_masterConfiguration.WebApi}";
            }

            return Task.CompletedTask;
        }
    }
}
