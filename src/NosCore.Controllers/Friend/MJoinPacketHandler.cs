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
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;

namespace NosCore.PacketHandlers.Friend
{
    public class MJoinPacketHandler : PacketHandler<MJoinPacket>, IWorldPacketHandler
    {
        private readonly IFriendHttpClient _friendHttpClient;
        private readonly IMinilandProvider _minilandProvider;

        public MJoinPacketHandler(IFriendHttpClient friendHttpClient, IMinilandProvider minilandProvider)
        {
            _friendHttpClient = friendHttpClient;
            _minilandProvider = minilandProvider;
        }

        public override async Task Execute(MJoinPacket mJoinPacket, ClientSession session)
        {
            var target = Broadcaster.Instance.GetCharacter(s => s.VisualId == mJoinPacket.VisualId);
            var friendList = await _friendHttpClient.GetListFriends(session.Character.CharacterId);
            if (target != null && friendList.Any(s => s.CharacterId == mJoinPacket.VisualId))
            {
                var miniland = _minilandProvider.GetMiniland(mJoinPacket.VisualId);
                if (miniland.State == MinilandState.Open)
                {
                    await session.ChangeMapInstance(miniland.MapInstanceId, 5, 8);
                }
                else
                {
                    if (miniland.State == MinilandState.Private &&
                        friendList.Where(w => w.RelationType != CharacterRelationType.Blocked)
                            .Select(s => s.CharacterId)
                            .ToList()
                            .Contains(target.VisualId))
                    {
                        await session.ChangeMapInstance(miniland.MapInstanceId, 5, 8);
                        return;
                    }
                    await session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_CLOSED_BY_FRIEND,
                                session.Account.Language)
                    });
                }
            }
        }
    }
}