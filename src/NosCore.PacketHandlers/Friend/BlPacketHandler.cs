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

using NosCore.GameObject;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Friend
{
    public class BlPacketHandler(ISessionRegistry sessionRegistry)
        : PacketHandler<BlPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(BlPacket finsPacket, ClientSession session)
        {
            var target =
                sessionRegistry.GetPlayer(s => s.Name == finsPacket.CharacterName);

            if (target is not { } player)
            {
                return session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
            }

            var blinsPacket = new BlInsPacket
            {
                CharacterId = player.VisualId
            };

            return session.HandlePacketsAsync(new[] { blinsPacket });
        }
    }
}