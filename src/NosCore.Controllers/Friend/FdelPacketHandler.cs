using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Friend
{
    public class FdelPacketHandler : PacketHandler<FdelPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        public FdelPacketHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override void Execute(FdelPacket fdelPacket, ClientSession session)
        {
            var friendServer = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)
                ?.FirstOrDefault(c => c.Type == ServerType.FriendServer);
            if (friendServer == null)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.FRIEND_SERVER_OFFLINE));
                return;
            }
            var list = WebApiAccess.Instance.Get<List<CharacterRelation>>(WebApiRoute.Friend, friendServer.WebApi,
                session.Character.VisualId);
            var idtorem = list.FirstOrDefault(s => s.RelatedCharacterId == fdelPacket.CharacterId);
            if (idtorem != null)
            {
                WebApiAccess.Instance.Delete<Guid>(WebApiRoute.Friend, friendServer.WebApi, idtorem.CharacterRelationId);
            }
            else
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_IN_BLACKLIST,
                        session.Account.Language)
                });
            }
        }
    }
}
