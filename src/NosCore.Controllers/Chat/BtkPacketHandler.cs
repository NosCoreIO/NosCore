using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Chat;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Chat
{
    public class BtkPacketHandler : PacketHandler<BtkPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly ISerializer _packetSerializer;
        public BtkPacketHandler(ILogger logger, ISerializer packetSerializer)
        {
            _logger = logger;
            _packetSerializer = packetSerializer;
        }

        public override void Execute(BtkPacket btkPacket, ClientSession session)
        {
            if (!session.Character.CharacterRelations.Values.Any(s =>
                     s.RelatedCharacterId == btkPacket.CharacterId && s.RelationType != CharacterRelationType.Blocked))
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

            ConnectedAccount receiver = null;

            var servers = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)
                ?.Where(c => c.Type == ServerType.WorldServer).ToList();
            foreach (var server in servers ?? new List<ChannelInfo>())
            {
                //TODO fix
                //var accounts = WebApiAccess.Instance
                //    .Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, server.WebApi);

                //if (accounts.Any(a => a.ConnectedCharacter?.Id == btkPacket.CharacterId))
                //{
                //    receiver = accounts.First(a => a.ConnectedCharacter?.Id == btkPacket.CharacterId);
                //}
            }

            if (receiver == null)
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_OFFLINE, session.Account.Language)
                });
                return;
            }

            WebApiAccess.Instance.BroadcastPacket(new PostedPacket
            {
                Packet = _packetSerializer.Serialize(new[] { session.Character.GenerateTalk(message) }),
                ReceiverCharacter = new Data.WebApi.Character
                { Id = btkPacket.CharacterId, Name = receiver.ConnectedCharacter?.Name },
                SenderCharacter = new Data.WebApi.Character
                { Name = session.Character.Name, Id = session.Character.CharacterId },
                OriginWorldId = MasterClientListSingleton.Instance.ChannelId,
                ReceiverType = ReceiverType.OnlySomeone
            }, receiver.ChannelId);

        }
    }
}
