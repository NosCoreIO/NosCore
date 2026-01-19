//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.Networking;
using NosCore.Shared.I18N;
using Polly;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.LoginServer
{
    public class LoginServer(IOptions<LoginConfiguration> loginConfiguration, NetworkManager networkManager,
            Serilog.ILogger logger, NosCoreContext context,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, Channel channel, IChannelHub channelHubClient)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title += $@" - Port : {Convert.ToInt32(loginConfiguration.Value.Port)}";
            }

            try
            {
                await context.Database.MigrateAsync(stoppingToken);
                await context.Database.GetDbConnection().OpenAsync(stoppingToken);
                logger.Information(logLanguage[LogLanguageKey.DATABASE_INITIALIZED]);
            }
            catch (Exception ex)
            {
                logger.Error(logLanguage[LogLanguageKey.DATABASE_ERROR], ex);
                logger.Error(logLanguage[LogLanguageKey.DATABASE_NOT_UPTODATE]);
                throw;
            }
            var connectTask = Policy
                 .Handle<Exception>()
                 .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                     (_, __, timeSpan) =>
                         logger.Error(
                             logLanguage[LogLanguageKey.MASTER_SERVER_RETRY],
                             timeSpan.TotalSeconds)
                 ).ExecuteAsync(() => channelHubClient.Bind(channel));

            await Task.WhenAny(connectTask, networkManager.RunServerAsync());
        }
    }
}
