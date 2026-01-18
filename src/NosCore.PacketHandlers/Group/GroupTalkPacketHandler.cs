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
using NosCore.GameObject.Ecs.Systems;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using System.Threading.Tasks;
using NosCore.Networking;
using NosCore.GameObject.Networking;


namespace NosCore.PacketHandlers.Group
{
    public class GroupTalkPacketHandler(IVisibilitySystem visibilitySystem) : PacketHandler<GroupTalkPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(GroupTalkPacket groupTalkPacket, ClientSession clientSession)
        {
            if (clientSession.Player.Group!.Count == 1)
            {
                return Task.CompletedTask;
            }

            return clientSession.Player.Group.SendPacketAsync(
                visibilitySystem.GenerateSpk(clientSession.Player, new SpeakPacket
                { Message = groupTalkPacket.Message, SpeakType = SpeakType.Group }));
        }
    }
}