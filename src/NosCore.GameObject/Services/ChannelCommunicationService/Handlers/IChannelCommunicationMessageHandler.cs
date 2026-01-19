//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using IMessage = NosCore.GameObject.InterChannelCommunication.Messages.IMessage;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers;

public interface IChannelCommunicationMessageHandler<in T> where T : IMessage
{
    Task Handle(T message);
}

public abstract class ChannelCommunicationMessageHandler<T> : IChannelCommunicationMessageHandler<IMessage> where T : IMessage
{
    public abstract Task Handle(T message);

    public Task Handle(IMessage message)
    {
        return Handle((T)message);
    }
}
