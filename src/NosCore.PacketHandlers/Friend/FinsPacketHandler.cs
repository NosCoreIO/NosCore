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

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;

namespace NosCore.PacketHandlers.Friend
{
    public class FinsPacketHandler(IFriendHub friendHttpClient, IChannelHub channelHttpClient,
            IPubSubHub pubSubHub, ISessionRegistry sessionRegistry, ICharacterPacketSystem characterPacketSystem)
        : PacketHandler<FinsPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(FinsPacket finsPacket, ClientSession session)
        {
            var targetCharacter = sessionRegistry.GetPlayer(s => s.VisualId == finsPacket.CharacterId);
            if (targetCharacter is {} target)
            {
                var result = await friendHttpClient.AddFriendAsync(new FriendShipRequest
                { CharacterId = session.Player.CharacterId, FinsPacket = finsPacket }).ConfigureAwait(false);

                var targetSender = sessionRegistry.GetSenderByCharacterId(target.VisualId);
                switch (result)
                {
                    case LanguageKey.FRIENDLIST_FULL:
                        await session.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.MaxFriendReachedAdd
                        }).ConfigureAwait(false);
                        break;

                    case LanguageKey.BLACKLIST_BLOCKED:
                        await session.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.AlreadyBlacklisted
                        }).ConfigureAwait(false);
                        break;

                    case LanguageKey.ALREADY_FRIEND:
                        await session.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.RegisteredAsFriend
                        }).ConfigureAwait(false);
                        break;

                    case LanguageKey.FRIEND_REQUEST_BLOCKED:
                        await session.SendPacketAsync(new Infoi2Packet
                        {
                            Message = Game18NConstString.HAsFriendRequestBlocked,
                            ArgumentType = 1,
                            Game18NArguments = { target.Name }
                        }).ConfigureAwait(false);
                        break;

                    case LanguageKey.FRIEND_REQUEST_SENT:
                        if (targetSender != null)
                        {
                            await targetSender.SendPacketAsync(new Dlgi2Packet
                            {
                                Question = Game18NConstString.AskBecomeFriend,
                                ArgumentType = 1,
                                Game18NArguments = { session.Player.Name },
                                YesPacket = new FinsPacket
                                { Type = FinsPacketType.Accepted, CharacterId = session.Player.VisualId },
                                NoPacket = new FinsPacket
                                { Type = FinsPacketType.Rejected, CharacterId = session.Player.VisualId }
                            }).ConfigureAwait(false);
                        }
                        break;

                    case LanguageKey.FRIEND_ADDED:
                        await session.SendPacketAsync(new InfoiPacket
                        {
                            Message = Game18NConstString.Registered
                        }).ConfigureAwait(false);
                        if (targetSender != null)
                        {
                            await targetSender.SendPacketAsync(new InfoiPacket
                            {
                                Message = Game18NConstString.Registered
                            }).ConfigureAwait(false);
                            await targetSender.SendPacketAsync(await characterPacketSystem.GenerateFinitAsync(target, friendHttpClient, channelHttpClient,
                                pubSubHub).ConfigureAwait(false)).ConfigureAwait(false);
                        }
                        await session.SendPacketAsync(await characterPacketSystem.GenerateFinitAsync(session.Player, friendHttpClient,
                            channelHttpClient, pubSubHub).ConfigureAwait(false)).ConfigureAwait(false);
                        break;

                    case LanguageKey.FRIEND_REJECTED:
                        if (targetSender != null)
                        {
                            await targetSender.SendPacketAsync(new InfoiPacket
                            {
                                Message = Game18NConstString.YouAreBlocked
                            }).ConfigureAwait(false);
                        }
                        break;

                    default:
                        throw new ArgumentException();
                }
            }
        }
    }
}