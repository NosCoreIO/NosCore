using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using NosCore.Core;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub
{
    public class ChannelHubClient(HubConnectionFactory hubConnectionFactory) : IChannelHub
    {
        private readonly HubConnection _hubConnection = hubConnectionFactory.Create(nameof(PubSubHub));

        public async Task Bind(Channel data)
        {
            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync(nameof(Bind), data);
        }

        public Task<List<ChannelInfo>> GetCommunicationChannels()
        {
            return _hubConnection.InvokeAsync<List<ChannelInfo>>(nameof(GetCommunicationChannels));
        }
    }
}