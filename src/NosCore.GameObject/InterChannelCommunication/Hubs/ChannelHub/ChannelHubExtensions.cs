//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NosCore.Core;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using Polly;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;

public static class ChannelHubExtensions
{
    // A channel may start before the master server is up, so it keeps trying with a growing delay.
    public static Task BindUntilConnectedAsync(this IChannelHub channelHub, Channel channel, ILogger logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage) =>
        Policy
            .Handle<Exception>()
            .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (_, __, timeSpan) =>
                    logger.LogError(
                        logLanguage[LogLanguageKey.MASTER_SERVER_RETRY],
                        timeSpan.TotalSeconds)
            ).ExecuteAsync(() => channelHub.Bind(channel));
}
