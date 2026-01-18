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

using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using Serilog;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using Character = NosCore.Data.WebApi.Character;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.BroadcastService;

namespace NosCore.PacketHandlers.Chat
{
    public class BtkPacketHandler(ILogger logger, ISerializer packetSerializer, IFriendHub friendHttpClient,
            IPubSubHub packetHttpClient, IPubSubHub pubSubHub, Channel channel,
            IGameLanguageLocalizer gameLanguageLocalizer, ISessionRegistry sessionRegistry)
        : PacketHandler<BtkPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(BtkPacket btkPacket, ClientSession session)
        {
            var friendlist = await friendHttpClient.GetFriendsAsync(session.Character.VisualId).ConfigureAwait(false);

            if (friendlist.All(s => s.CharacterId != btkPacket.CharacterId))
            {
                logger.Error(gameLanguageLocalizer[LanguageKey.USER_IS_NOT_A_FRIEND,
                    session.Account.Language]);
                return;
            }

            var message = btkPacket.Message ?? "";
            if (message.Length > 60)
            {
                message = message.Substring(0, 60);
            }

            message = message.Trim();
            var receiverSession =
                sessionRegistry.GetCharacter(s =>
                    s.VisualId == btkPacket.CharacterId);

            if (receiverSession != null)
            {
                await receiverSession.SendPacketAsync(session.Character.GenerateTalk(message)).ConfigureAwait(false);
                return;
            }

            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(x => x.ConnectedCharacter?.Id == btkPacket.CharacterId);

            if (receiver == null)
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.FriendOffline
                }).ConfigureAwait(false);
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
            }).ConfigureAwait(false);
        }
    }
}