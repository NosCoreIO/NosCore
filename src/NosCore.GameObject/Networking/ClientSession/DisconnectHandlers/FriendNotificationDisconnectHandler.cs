//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.Packets.Interfaces;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class FriendNotificationDisconnectHandler(IFriendHub friendHub, IPubSubHub pubSubHub, ISerializer serializer) : ISessionDisconnectHandler
{
    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter)
        {
            return;
        }

        await session.Character.SendFinfoAsync(friendHub, pubSubHub, serializer, false);
    }
}
