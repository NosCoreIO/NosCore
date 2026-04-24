//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Chat
{
    public class BtkPacketHandler(ILogger<BtkPacketHandler> logger, ISerializer packetSerializer, IFriendHub friendHttpClient,
            IPubSubHub packetHttpClient, IPubSubHub pubSubHub, Channel channel,
            IGameLanguageLocalizer gameLanguageLocalizer, ISessionRegistry sessionRegistry)
        : PacketHandler<BtkPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(BtkPacket btkPacket, ClientSession session)
        {
            var friendlist = await friendHttpClient.GetFriendsAsync(session.Character.VisualId);

            if (friendlist.All(s => s.CharacterId != btkPacket.CharacterId))
            {
                logger.LogError(gameLanguageLocalizer[LanguageKey.USER_IS_NOT_A_FRIEND,
                    session.Account.Language]);
                return;
            }

            var message = btkPacket.Message ?? "";
            if (message.Length > 60)
            {
                message = message.Substring(0, 60);
            }

            message = message.Trim();
            if (sessionRegistry.TryGetCharacter(s => s.VisualId == btkPacket.CharacterId, out var receiverSession))
            {
                await receiverSession.SendPacketAsync(session.Character.GenerateTalk(message));
                return;
            }

            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(x => x.ConnectedCharacter?.Id == btkPacket.CharacterId);

            if (receiver == null)
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.FriendOffline
                });
                return;
            }

            await packetHttpClient.SendMessageAsync(new PostedPacket
            {
                Packet = packetSerializer.Serialize(new[] { session.Character.GenerateTalk(message) }),
                ReceiverCharacter = new Character
                { Id = btkPacket.CharacterId, Name = receiver.ConnectedCharacter?.Name ?? "" },
                SenderCharacter = new Character
                { Name = session.Character.Name, Id = session.Character.CharacterId },
                OriginWorldId = channel.ChannelId,
                ReceiverType = ReceiverType.OnlySomeone
            });
        }
    }
}
