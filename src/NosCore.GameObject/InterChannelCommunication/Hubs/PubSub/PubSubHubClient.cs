using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Messages;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.PubSub
{
    public class PubSubHubClient(HubConnectionFactory hubConnectionFactory) : IPubSubHub
    {
        private readonly HubConnection _hubConnection = hubConnectionFactory.Create(nameof(PubSubHub));


        public async Task<List<IMessage>> ReceiveMessagesAsync()
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<List<IMessage>>(nameof(ReceiveMessagesAsync));
            await _hubConnection.StopAsync();
            return result;
        }

        public async Task<bool> DeleteMessageAsync(Guid messageId)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<bool>(nameof(DeleteMessageAsync), messageId);
            await _hubConnection.StopAsync();
            return result;
        }

        public async Task<bool> SendMessageAsync(IMessage message)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<bool>(nameof(SendMessageAsync), message);
            await _hubConnection.StopAsync();
            return result;
        }

        public async Task<bool> SendMessagesAsync(List<IMessage> messages)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<bool>(nameof(SendMessagesAsync), messages);
            await _hubConnection.StopAsync();
            return result;
        }

        public async Task<List<Subscriber>> GetSubscribersAsync()
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<List<Subscriber>>(nameof(GetSubscribersAsync));
            await _hubConnection.StopAsync();
            return result;
        }

        public async Task<bool> SubscribeAsync(Subscriber subscriber)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<bool>(nameof(SubscribeAsync), subscriber);
            await _hubConnection.StopAsync();
            return result;
        }

        public async Task<bool> UnsubscribeAsync(long id)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<bool>(nameof(UnsubscribeAsync), id);
            await _hubConnection.StopAsync();
            return result;
        }
    }
}