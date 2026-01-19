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
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.GameObject.Services.BroadcastService;

namespace NosCore.PacketHandlers.Miniland
{
    public class MJoinPacketHandler(IFriendHub friendHttpClient, IMinilandService minilandProvider,
            IMapChangeService mapChangeService, ISessionRegistry sessionRegistry)
        : PacketHandler<MJoinPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(MJoinPacket mJoinPacket, ClientSession session)
        {
            var target = sessionRegistry.GetCharacter(s => s.VisualId == mJoinPacket.VisualId);
            var friendList = await friendHttpClient.GetFriendsAsync(session.Character.CharacterId);
            if (target != null && friendList.Any(s => s.CharacterId == mJoinPacket.VisualId))
            {
                var miniland = minilandProvider.GetMiniland(mJoinPacket.VisualId);
                if (miniland.State == MinilandState.Open)
                {
                    await mapChangeService.ChangeMapInstanceAsync(session, miniland.MapInstanceId, 5, 8);
                }
                else
                {
                    if (miniland.State == MinilandState.Private &&
                        friendList.Where(w => w.RelationType != CharacterRelationType.Blocked)
                            .Select(s => s.CharacterId)
                            .ToList()
                            .Contains(target.VisualId))
                    {
                        await mapChangeService.ChangeMapInstanceAsync(session, miniland.MapInstanceId, 5, 8);
                        return;
                    }
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.MinilandLocked
                    });
                }
            }
        }
    }
}