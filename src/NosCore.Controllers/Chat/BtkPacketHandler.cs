using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Chat;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.HttpClients.PacketHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Chat
{
    public class BtkPacketHandler : PacketHandler<BtkPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly ISerializer _packetSerializer;
        private readonly IFriendHttpClient _friendHttpClient;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly IPacketHttpClient _packetHttpClient;
        public BtkPacketHandler(ILogger logger, ISerializer packetSerializer, IFriendHttpClient friendHttpClient,
            IPacketHttpClient packetHttpClient, IConnectedAccountHttpClient connectedAccountHttpClient)
        {
            _logger = logger;
            _packetSerializer = packetSerializer;
            _friendHttpClient = friendHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _packetHttpClient = packetHttpClient;
        }

        public override void Execute(BtkPacket btkPacket, ClientSession session)
        {
            var friendlist = _friendHttpClient.GetListFriends(session.Character.VisualId);

            if (!friendlist.Any(s => s.CharacterId == btkPacket.CharacterId))
            {
                _logger.Error(Language.Instance.GetMessageFromKey(LanguageKey.USER_IS_NOT_A_FRIEND,
                    session.Account.Language));
                return;
            }

            var message = btkPacket.Message;
            if (message.Length > 60)
            {
                message = message.Substring(0, 60);
            }

            message = message.Trim();
            var receiverSession =
                Broadcaster.Instance.GetCharacter(s =>
                    s.VisualId == btkPacket.CharacterId);

            if (receiverSession != null)
            {
                receiverSession.SendPacket(session.Character.GenerateTalk(message));
                return;
            }

            var receiver =  _connectedAccountHttpClient.GetCharacter(btkPacket.CharacterId, null);

            if (receiver.Item2 == null) //TODO: Handle 404 in WebApi
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_OFFLINE, session.Account.Language)
                });
                return;
            }

            _packetHttpClient.BroadcastPacket(new PostedPacket
            {
                Packet = _packetSerializer.Serialize(new[] { session.Character.GenerateTalk(message) }),
                ReceiverCharacter = new Data.WebApi.Character
                { Id = btkPacket.CharacterId, Name = receiver.Item2.ConnectedCharacter?.Name },
                SenderCharacter = new Data.WebApi.Character
                { Name = session.Character.Name, Id = session.Character.CharacterId },
                OriginWorldId = MasterClientListSingleton.Instance.ChannelId,
                ReceiverType = ReceiverType.OnlySomeone
            }, receiver.Item2.ChannelId);

        }
    }
}
