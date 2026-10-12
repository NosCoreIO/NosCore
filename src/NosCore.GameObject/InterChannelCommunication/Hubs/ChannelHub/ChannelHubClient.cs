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
using System.Collections.Generic;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub
{
    public class ChannelHubClient(IMessageBus bus, InterChannelNode node, ILogger<ChannelHubClient> logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : IChannelHub
    {
        public async Task Bind(Channel data)
        {
            await bus.SendToMasterAsync(new BindChannelRequest(node.QueueName, WithoutCredentials(data)));
            logger.LogDebug(logLanguage[LogLanguageKey.REGISTRED_ON_MASTER]);

            await Policy
                .HandleResult<bool>(ping => ping)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(1),
                    (_, __, timeSpan) =>
                        logger.LogTrace(
                            logLanguage[LogLanguageKey.MASTER_SERVER_PING])
                ).ExecuteAsync(Ping);
            logger.LogError(
                logLanguage[LogLanguageKey.MASTER_SERVER_PING_FAILED]);
            Environment.Exit(0);
        }

        public Task<List<ChannelInfo>> GetCommunicationChannels() =>
            bus.RequestAsync<List<ChannelInfo>>(new GetCommunicationChannelsRequest());

        public Task<bool> Ping() => bus.RequestAsync<bool>(new PingChannelRequest(node.QueueName));

        public Task SetMaintenance(bool isGlobal, bool value) =>
            bus.SendToMasterAsync(new SetMaintenanceRequest(node.QueueName, isGlobal, value));

        private static Channel WithoutCredentials(Channel data) => new()
        {
            ClientName = data.ClientName,
            ClientType = data.ClientType,
            Port = data.Port,
            ServerId = data.ServerId,
            ChannelId = data.ChannelId,
            DisplayHost = data.DisplayHost,
            DisplayPort = data.DisplayPort,
            StartInMaintenance = data.StartInMaintenance,
            Host = data.Host,
            ConnectedAccountLimit = data.ConnectedAccountLimit,
        };
    }
}
