//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Relations;
using System.Collections.Generic;
using System.Linq;
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

        var character = session.Character;
        var characterId = character.CharacterId;
        var friends = await friendHub.GetFriendsAsync(characterId);

        var disconnectPacket = serializer.Serialize(new[]
        {
            new FinfoPacket
            {
                FriendList = new List<FinfoSubPackets?>
                {
                    new()
                    {
                        CharacterId = characterId,
                        IsConnected = false
                    }
                }
            }
        });

        var messages = friends.Select(friend => new PostedPacket
        {
            ReceiverType = ReceiverType.OnlySomeone,
            SenderCharacter = new Data.WebApi.Character { Id = characterId },
            ReceiverCharacter = new Data.WebApi.Character { Id = friend.CharacterId },
            Packet = disconnectPacket
        }).Cast<IMessage>().ToList();

        if (messages.Count > 0)
        {
            await pubSubHub.SendMessagesAsync(messages);
        }
    }
}
