//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;

public interface IPubSubHub
{
    Task<bool> SendMessageAsync(IMessage message);

    Task<bool> SendMessagesAsync(List<IMessage> messages);

    Task<List<Subscriber>> GetSubscribersAsync();

    Task<bool> SubscribeAsync(Subscriber subscriber);

    Task<bool> UnsubscribeAsync(long id);
}

public interface IPubSubHubClient : IPubSubHub
{
    event Action<IMessage>? OnMessageReceived;

    Task StartAsync();

    Task StopAsync();

    bool IsConnected { get; }
}
