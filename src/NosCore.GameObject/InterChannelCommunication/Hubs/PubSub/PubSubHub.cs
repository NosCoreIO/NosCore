//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.SignalR;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.PubSub
{
    public class PubSubHub(MasterClientList masterClientList)
        : Hub, IPubSubHub
    {
        public async Task<bool> SendMessageAsync(IMessage message)
        {
            await Clients.Others.SendAsync("ReceiveMessage", message);
            return true;
        }

        public async Task<bool> SendMessagesAsync(List<IMessage> messages)
        {
            foreach (var message in messages)
            {
                await Clients.Others.SendAsync("ReceiveMessage", message);
            }
            return true;
        }

        public Task<List<Subscriber>> GetSubscribersAsync()
        {
            return Task.FromResult(masterClientList.ConnectedAccounts.SelectMany(x => x.Value.Values).ToList());
        }

        public Task<bool> SubscribeAsync(Subscriber subscriber)
        {
            if (!masterClientList.ConnectedAccounts.ContainsKey(Context.UserIdentifier ?? throw new InvalidOperationException())
                || !masterClientList.Channels.ContainsKey(Context.UserIdentifier ?? throw new InvalidOperationException()))
            {
                return Task.FromResult(false);
            }
            subscriber.ChannelId = masterClientList.Channels[Context.UserIdentifier].Id;
            masterClientList.ConnectedAccounts[Context.UserIdentifier].AddOrUpdate(subscriber.Id, subscriber, (_, _) => subscriber);
            return Task.FromResult(true);
        }

        public Task<bool> UnsubscribeAsync(long id)
        {
            if (!masterClientList.ConnectedAccounts.ContainsKey(Context.UserIdentifier ?? throw new InvalidOperationException()))
            {
                return Task.FromResult(false);
            }
            return Task.FromResult(masterClientList.ConnectedAccounts[Context.UserIdentifier].TryRemove(id, out _));
        }
    }
}
