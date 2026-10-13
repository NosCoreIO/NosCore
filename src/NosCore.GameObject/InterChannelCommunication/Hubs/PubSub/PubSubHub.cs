//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using NosCore.Data.WebApi;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wolverine;
using IMessage = NosCore.GameObject.InterChannelCommunication.Messages.IMessage;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.PubSub
{
    public class PubSubHub(MasterClientList masterClientList, IMessageBus bus, MessagingConfiguration messaging)
        : IPubSubHub
    {
        public Task<bool> SendMessageAsync(IMessage message) => SendMessagesAsync([message]);

        public Task<bool> SendMessagesAsync(List<IMessage> messages) => BroadcastAsync(messages);

        public Task<List<Subscriber>> GetSubscribersAsync()
        {
            return Task.FromResult(masterClientList.ConnectedAccounts.SelectMany(x => x.Value.Values).ToList());
        }

        public Task<bool> SubscribeAsync(Subscriber subscriber) =>
            throw new InvalidOperationException("The master server is not a channel");

        public Task<bool> UnsubscribeAsync(long id) =>
            throw new InvalidOperationException("The master server is not a channel");

        public bool Subscribe(string channelKey, Subscriber subscriber)
        {
            if (!masterClientList.ConnectedAccounts.TryGetValue(channelKey, out var accounts)
                || !masterClientList.Channels.TryGetValue(channelKey, out var channel))
            {
                return false;
            }

            subscriber.ChannelId = channel.Id;
            accounts.AddOrUpdate(subscriber.Id, subscriber, (_, _) => subscriber);
            return true;
        }

        public bool Unsubscribe(string channelKey, long id) =>
            masterClientList.ConnectedAccounts.TryGetValue(channelKey, out var accounts)
            && accounts.TryRemove(id, out _);

        public static async Task<Reply<bool>> Handle(PublishMessagesRequest request, IServiceProvider services) =>
            new(await services.GetRequiredService<PubSubHub>().BroadcastAsync(request.Messages));

        public static async Task<Reply<List<Subscriber>>> Handle(GetSubscribersRequest _, IServiceProvider services) =>
            new(await services.GetRequiredService<PubSubHub>().GetSubscribersAsync());

        public static Reply<bool> Handle(SubscribeRequest request, IServiceProvider services) =>
            new(services.GetRequiredService<PubSubHub>().Subscribe(request.ChannelKey, request.Subscriber));

        public static Reply<bool> Handle(UnsubscribeRequest request, IServiceProvider services) =>
            new(services.GetRequiredService<PubSubHub>().Unsubscribe(request.ChannelKey, request.Id));

        // The sending channel is a recipient too: a whisper, shout or GM command aimed at someone
        // on the sender's own channel is only delivered through this round trip.
        private async Task<bool> BroadcastAsync(List<IMessage> messages)
        {
            var recipients = masterClientList.Channels
                .Where(c => c.Value.Type == ServerType.WorldServer)
                .Select(c => c.Key);
            foreach (var channelKey in recipients)
            {
                var endpoint = bus.EndpointFor(messaging.QueueUri(channelKey));
                foreach (var message in messages)
                {
                    await endpoint.SendAsync(new ChannelMessage(message));
                }
            }

            return true;
        }
    }
}
