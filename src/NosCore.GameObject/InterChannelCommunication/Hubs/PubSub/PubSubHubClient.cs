//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wolverine;
using IMessage = NosCore.GameObject.InterChannelCommunication.Messages.IMessage;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.PubSub
{
    public class PubSubHubClient(IMessageBus bus, InterChannelNode node) : IPubSubHub
    {
        public Task<bool> SendMessageAsync(IMessage message) => SendMessagesAsync([message]);

        public Task<bool> SendMessagesAsync(List<IMessage> messages) =>
            bus.RequestAsync<bool>(new PublishMessagesRequest(node.QueueName, messages));

        public Task<List<Subscriber>> GetSubscribersAsync() =>
            bus.RequestAsync<List<Subscriber>>(new GetSubscribersRequest());

        public Task<bool> SubscribeAsync(Subscriber subscriber) =>
            bus.RequestAsync<bool>(new SubscribeRequest(node.QueueName, subscriber));

        public Task<bool> UnsubscribeAsync(long id) =>
            bus.RequestAsync<bool>(new UnsubscribeRequest(node.QueueName, id));
    }
}
