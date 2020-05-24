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

using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.HttpClients.PacketHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Chat
{
    public class BtkPacketHandler : PacketHandler<BtkPacket>, IWorldPacketHandler
    {
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly IFriendHttpClient _friendHttpClient;
        private readonly ILogger _logger;
        private readonly IPacketHttpClient _packetHttpClient;
        private readonly ISerializer _packetSerializer;

        public BtkPacketHandler(ILogger logger, ISerializer packetSerializer, IFriendHttpClient friendHttpClient,
            IPacketHttpClient packetHttpClient, IConnectedAccountHttpClient connectedAccountHttpClient)
        {
            _logger = logger;
            _packetSerializer = packetSerializer;
            _friendHttpClient = friendHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _packetHttpClient = packetHttpClient;
        }

        public override async Task ExecuteAsync(BtkPacket btkPacket, ClientSession session)
        {
            var friendlist = await _friendHttpClient.GetListFriendsAsync(session.Character.VisualId).ConfigureAwait(false);

            if (friendlist.All(s => s.CharacterId != btkPacket.CharacterId))
            {
                _logger.Error(GameLanguage.Instance.GetMessageFromKey(LanguageKey.USER_IS_NOT_A_FRIEND,
                    session.Account.Language));
                return;
            }

            var message = btkPacket.Message ?? "";
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
                await receiverSession.SendPacketAsync(session.Character.GenerateTalk(message)).ConfigureAwait(false);
                return;
            }

            var receiver = await _connectedAccountHttpClient.GetCharacterAsync(btkPacket.CharacterId, null).ConfigureAwait(false);

            if (receiver.Item2 == null) //TODO: Handle 404 in WebApi
            {
                await session.SendPacketAsync(new InfoPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.FRIEND_OFFLINE, session.Account.Language)
                }).ConfigureAwait(false);
                return;
            }

            await _packetHttpClient.BroadcastPacketAsync(new PostedPacket
            {
                Packet = _packetSerializer.Serialize(new[] { session.Character.GenerateTalk(message) }),
                ReceiverCharacter = new Character
                    { Id = btkPacket.CharacterId, Name = receiver.Item2.ConnectedCharacter?.Name ?? "" },
                SenderCharacter = new Character
                    { Name = session.Character.Name, Id = session.Character.CharacterId },
                OriginWorldId = MasterClientListSingleton.Instance.ChannelId,
                ReceiverType = ReceiverType.OnlySomeone
            }, receiver.Item2.ChannelId).ConfigureAwait(false);
        }
    }
}