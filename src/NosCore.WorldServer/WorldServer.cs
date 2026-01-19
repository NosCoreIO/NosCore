//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.EventLoaderService.Handlers;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup;
using NosCore.Shared.I18N;
using Polly;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.WorldServer
{
    public class WorldServer(IOptions<WorldConfiguration> worldConfiguration, NetworkManager networkManager,
            Clock clock, ILogger<WorldServer> logger, IMapInstanceGeneratorService mapInstanceGeneratorService,
            IClock nodatimeClock, ISaveService saveService,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, ILogger<SaveAll> saveAllLogger, Channel channel, IChannelHub channelHubClient,
            ISessionGroupFactory sessionGroupFactory, ISessionRegistry sessionRegistry)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Broadcaster.Initialize(sessionGroupFactory);
            await mapInstanceGeneratorService.InitializeAsync();
            logger.LogInformation(logLanguage[LogLanguageKey.SUCCESSFULLY_LOADED]);
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                var eventSaveAll = new SaveAll(saveAllLogger, nodatimeClock, saveService, logLanguage, sessionRegistry);
                _ = eventSaveAll.ExecuteAsync();
                logger.LogInformation(logLanguage[LogLanguageKey.CHANNEL_WILL_EXIT], 30);
                Thread.Sleep(30000);
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title += $@" - Port : {worldConfiguration.Value.Port}";
            }
            var connectTask = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (_, __, timeSpan) =>
                        logger.LogError(
                            logLanguage[LogLanguageKey.MASTER_SERVER_RETRY],
                            timeSpan.TotalSeconds)
                ).ExecuteAsync(() => channelHubClient.Bind(channel));
            await Task.WhenAny(connectTask, clock.Run(stoppingToken), networkManager.RunServerAsync());
        }
    }
}
