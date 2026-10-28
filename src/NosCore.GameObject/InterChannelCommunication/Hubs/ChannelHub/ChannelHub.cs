//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Core;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Shared.I18N;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Wolverine.Attributes;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub
{
    public class ChannelHub(ILogger<ChannelHub> logger, MasterClientList masterClientList,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IClock clock)
        : IChannelHub
    {
        public Task Bind(Channel data) => throw new InvalidOperationException("The master server is not a channel");

        public Task<bool> Ping() => throw new InvalidOperationException("The master server is not a channel");

        public Task<List<ChannelInfo>> GetCommunicationChannels()
        {
            return Task.FromResult(masterClientList.Channels.Values.ToList());
        }

        public Task SetMaintenance(bool isGlobal, bool value)
        {
            if (!isGlobal)
            {
                throw new InvalidOperationException("The master server is not a channel");
            }

            foreach (var channel in masterClientList.Channels)
            {
                channel.Value.IsMaintenance = value;
            }

            return Task.CompletedTask;
        }

        public void BindChannel(string channelKey, Channel data)
        {
            var id = ++masterClientList.ConnectionCounter;
            logger.LogDebug(logLanguage[LogLanguageKey.AUTHENTICATED_SUCCESS],
                id.ToString(CultureInfo.CurrentCulture),
                data.ClientName);
            masterClientList.ConnectedAccounts.TryAdd(channelKey, new ConcurrentDictionary<long, Subscriber>());
            var serv = new ChannelInfo
            {
                Name = data.ClientName,
                Host = data.Host,
                Port = data.Port,
                DisplayPort = (ushort?)data.DisplayPort,
                DisplayHost = data.DisplayHost,
                IsMaintenance = data.StartInMaintenance,
                Id = id,
                ConnectedAccountLimit = data.ConnectedAccountLimit,
                Type = data.ClientType,
            };
            masterClientList.Channels.AddOrUpdate(channelKey, serv, (_, _) => serv);
            masterClientList.LastSeen[channelKey] = clock.GetCurrentInstant();
        }

        public void SetChannelMaintenance(string channelKey, bool value)
        {
            if (masterClientList.Channels.TryGetValue(channelKey, out var channel))
            {
                channel.IsMaintenance = value;
            }
        }

        public bool PingChannel(string channelKey)
        {
            if (!masterClientList.Channels.ContainsKey(channelKey))
            {
                return false;
            }

            masterClientList.LastSeen[channelKey] = clock.GetCurrentInstant();
            return true;
        }

        public void RemoveChannel(string channelKey)
        {
            masterClientList.LastSeen.TryRemove(channelKey, out _);
            if (!masterClientList.Channels.TryRemove(channelKey, out var channel))
            {
                return;
            }

            logger.LogDebug(logLanguage[LogLanguageKey.CONNECTION_LOST],
                channel.Id.ToString(CultureInfo.CurrentCulture),
                channel.Name);
            masterClientList.ConnectedAccounts.TryRemove(channelKey, out _);
        }

        public static void Handle(BindChannelRequest request, IServiceProvider services) =>
            services.GetRequiredService<ChannelHub>().BindChannel(request.ChannelKey, request.Channel);

        public static async Task<Reply<List<ChannelInfo>>> Handle(GetCommunicationChannelsRequest _, IServiceProvider services) =>
            new(await services.GetRequiredService<ChannelHub>().GetCommunicationChannels());

        public static Task Handle(SetMaintenanceRequest request, IServiceProvider services)
        {
            var hub = services.GetRequiredService<ChannelHub>();
            if (request.IsGlobal)
            {
                return hub.SetMaintenance(true, request.Value);
            }

            hub.SetChannelMaintenance(request.ChannelKey, request.Value);
            return Task.CompletedTask;
        }

        [WolverineLogging(telemetryEnabled: false, successLogLevel: LogLevel.None, executionLogLevel: LogLevel.None)]
        public static PingChannelReply Handle(PingChannelRequest request, IServiceProvider services) =>
            new(services.GetRequiredService<ChannelHub>().PingChannel(request.ChannelKey));
    }
}
