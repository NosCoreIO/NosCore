using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Friend
{
    public class FdelPacketHandler : PacketHandler<FdelPacket>, IWorldPacketHandler
    {
        private readonly IFriendHttpClient _friendHttpClient;
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        public FdelPacketHandler(IFriendHttpClient friendHttpClient, IChannelHttpClient channelHttpClient, IConnectedAccountHttpClient connectedAccountHttpClient)
        {
            _friendHttpClient = friendHttpClient;
            _channelHttpClient = channelHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
        }

        public override void Execute(FdelPacket fdelPacket, ClientSession session)
        {
            var list = _friendHttpClient.GetListFriends(session.Character.VisualId);
            var idtorem = list.FirstOrDefault(s => s.CharacterId == fdelPacket.CharacterId);
            if (idtorem != null)
            {
                _friendHttpClient.DeleteFriend(idtorem.CharacterRelationId);
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_DELETED, session.Account.Language)
                });
                var targetCharacter = Broadcaster.Instance.GetCharacter(s => s.VisualId == fdelPacket.CharacterId);
                if ( targetCharacter != null)
                {
                    targetCharacter.SendPacket(targetCharacter.GenerateFinit(_friendHttpClient,_channelHttpClient, _connectedAccountHttpClient));
                }

                session.Character.SendPacket(session.Character.GenerateFinit(_friendHttpClient, _channelHttpClient, _connectedAccountHttpClient));
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
