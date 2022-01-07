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
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.PacketHandlers.Miniland
{
    public class MJoinPacketHandler : PacketHandler<MJoinPacket>, IWorldPacketHandler
    {
        private readonly IFriendHttpClient _friendHttpClient;
        private readonly IMinilandService _minilandProvider;
        private readonly IMapChangeService _mapChangeService;

        public MJoinPacketHandler(IFriendHttpClient friendHttpClient, IMinilandService minilandProvider, IMapChangeService mapChangeService)
        {
            _friendHttpClient = friendHttpClient;
            _minilandProvider = minilandProvider;
            _mapChangeService = mapChangeService;
        }

        public override async Task ExecuteAsync(MJoinPacket mJoinPacket, ClientSession session)
        {
            var target = Broadcaster.Instance.GetCharacter(s => s.VisualId == mJoinPacket.VisualId);
            var friendList = await _friendHttpClient.GetListFriendsAsync(session.Character.CharacterId).ConfigureAwait(false);
            if (target != null && friendList.Any(s => s.CharacterId == mJoinPacket.VisualId))
            {
                var miniland = _minilandProvider.GetMiniland(mJoinPacket.VisualId);
                if (miniland.State == MinilandState.Open)
                {
                    await _mapChangeService.ChangeMapInstanceAsync(session, miniland.MapInstanceId, 5, 8).ConfigureAwait(false);
                }
                else
                {
                    if (miniland.State == MinilandState.Private &&
                        friendList.Where(w => w.RelationType != CharacterRelationType.Blocked)
                            .Select(s => s.CharacterId)
                            .ToList()
                            .Contains(target.VisualId))
                    {
                        await _mapChangeService.ChangeMapInstanceAsync(session, miniland.MapInstanceId, 5, 8).ConfigureAwait(false);
                        return;
                    }
                    await session.SendPacketAsync(new InfoPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.MINILAND_CLOSED_BY_FRIEND,
                            session.Account.Language)
                    }).ConfigureAwait(false);
                }
            }
        }
    }
}