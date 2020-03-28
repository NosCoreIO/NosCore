//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Friend
{
    public class FinsPacketHandler : PacketHandler<FinsPacket>, IWorldPacketHandler
    {
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly IFriendHttpClient _friendHttpClient;

        public FinsPacketHandler(IFriendHttpClient friendHttpClient, IChannelHttpClient channelHttpClient,
            IConnectedAccountHttpClient connectedAccountHttpClient)
        {
            _friendHttpClient = friendHttpClient;
            _channelHttpClient = channelHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
        }

        public override async Task Execute(FinsPacket finsPacket, ClientSession session)
        {
            var targetCharacter = Broadcaster.Instance.GetCharacter(s => s.VisualId == finsPacket.CharacterId);
            if (targetCharacter != null)
            {
                var result = await _friendHttpClient.AddFriend(new FriendShipRequest
                    {CharacterId = session.Character!.CharacterId, FinsPacket = finsPacket});

                switch (result)
                {
                    case LanguageKey.FRIENDLIST_FULL:
                        await session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIENDLIST_FULL,
                                session.Character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.BLACKLIST_BLOCKED:
                        await session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                                session.Character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.ALREADY_FRIEND:
                        await session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_FRIEND,
                                session.Character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.FRIEND_REQUEST_BLOCKED:
                        await session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_BLOCKED,
                                session.Character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.FRIEND_REQUEST_SENT:
                        await session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_SENT,
                                session.Character.AccountLanguage)
                        });
                        await targetCharacter.SendPacket(new DlgPacket
                        {
                            Question = string.Format(
                                Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADD,
                                    session.Character.AccountLanguage),
                                session.Character.Name),
                            YesPacket = new FinsPacket
                                {Type = FinsPacketType.Accepted, CharacterId = session.Character.VisualId},
                            NoPacket = new FinsPacket
                                {Type = FinsPacketType.Rejected, CharacterId = session.Character.VisualId}
                        });
                        break;

                    case LanguageKey.FRIEND_ADDED:
                        await session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                session.Character.AccountLanguage)
                        });
                        await targetCharacter.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                session.Character.AccountLanguage)
                        });

                        await targetCharacter.SendPacket(await targetCharacter.GenerateFinit(_friendHttpClient, _channelHttpClient,
                            _connectedAccountHttpClient));
                        await session.Character.SendPacket(await session.Character.GenerateFinit(_friendHttpClient,
                            _channelHttpClient, _connectedAccountHttpClient));
                        break;

                    case LanguageKey.FRIEND_REJECTED:
                        await targetCharacter.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REJECTED,
                                session.Character.AccountLanguage)
                        });
                        break;

                    default:
                        throw new ArgumentException();
                }
            }
        }
    }
}