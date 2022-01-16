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

using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Friend
{
    public class FdelPacketHandler : PacketHandler<FdelPacket>, IWorldPacketHandler
    {
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IConnectedAccountHttpClient _connectedAccountHttpClient;
        private readonly IFriendHttpClient _friendHttpClient;
        private readonly IGameLanguageLocalizer _gameLanguageLocalizer;

        public FdelPacketHandler(IFriendHttpClient friendHttpClient, IChannelHttpClient channelHttpClient,
            IConnectedAccountHttpClient connectedAccountHttpClient,  IGameLanguageLocalizer gameLanguageLocalizer)
        {
            _friendHttpClient = friendHttpClient;
            _channelHttpClient = channelHttpClient;
            _connectedAccountHttpClient = connectedAccountHttpClient;
            _gameLanguageLocalizer = gameLanguageLocalizer;
        }

        public override async Task ExecuteAsync(FdelPacket fdelPacket, ClientSession session)
        {
            var list = await _friendHttpClient.GetListFriendsAsync(session.Character.VisualId).ConfigureAwait(false);
            var idtorem = list.FirstOrDefault(s => s.CharacterId == fdelPacket.CharacterId);
            if (idtorem != null)
            {
                await _friendHttpClient.DeleteFriendAsync(idtorem.CharacterRelationId).ConfigureAwait(false);
                var targetCharacter = Broadcaster.Instance.GetCharacter(s => s.VisualId == fdelPacket.CharacterId);
                await (targetCharacter == null ? Task.CompletedTask : targetCharacter.SendPacketAsync(await targetCharacter.GenerateFinitAsync(_friendHttpClient, _channelHttpClient,
                    _connectedAccountHttpClient).ConfigureAwait(false))).ConfigureAwait(false);

                await session.Character.SendPacketAsync(await session.Character.GenerateFinitAsync(_friendHttpClient, _channelHttpClient,
                    _connectedAccountHttpClient).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                await session.SendPacketAsync(new InfoPacket
                {
                    Message = _gameLanguageLocalizer[LanguageKey.NOT_IN_FRIENDLIST,
                        session.Account.Language]
                }).ConfigureAwait(false);
            }
        }
    }
}