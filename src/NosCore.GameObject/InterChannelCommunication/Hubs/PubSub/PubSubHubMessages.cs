//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Messages;
using System.Collections.Generic;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;

public record PublishMessagesRequest(List<IMessage> Messages) : IMasterRequest;

public record GetSubscribersRequest : IMasterRequest;

public record SubscribeRequest(string ChannelKey, Subscriber Subscriber) : IMasterRequest;

public record UnsubscribeRequest(string ChannelKey, long Id) : IMasterRequest;

public record ChannelMessage(IMessage Message);
