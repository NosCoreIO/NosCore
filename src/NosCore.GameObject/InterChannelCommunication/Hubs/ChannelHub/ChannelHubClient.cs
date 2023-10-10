using System.Collections.Generic;
using System.Threading.Tasks;
using Json.Patch;
using Microsoft.AspNetCore.SignalR.Client;
using NosCore.Core;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub
{
    public class ChannelHubClient(HubConnectionFactory hubConnectionFactory, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IChannelHub
    {
        private readonly HubConnection _hubConnection = hubConnectionFactory.Create(nameof(ChannelHub));

        public async Task Bind(Channel data)
        {
            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync(nameof(Bind), data);
            logger.Debug(logLanguage[LogLanguageKey.REGISTRED_ON_MASTER]);
        }

        public Task<List<ChannelInfo>> GetCommunicationChannels()
        {
            return _hubConnection.InvokeAsync<List<ChannelInfo>>(nameof(GetCommunicationChannels));
        }

        public Task SetMaintenance(bool isGlobal, bool value)
        {
            return _hubConnection.InvokeAsync(nameof(SetMaintenance), isGlobal, value);
        }
    }
}