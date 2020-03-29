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

using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;

namespace NosCore.PacketHandlers.Chat
{
    public class ClientSayPacketHandler : PacketHandler<ClientSayPacket>, IWorldPacketHandler
    {
        public override async Task Execute(ClientSayPacket clientSayPacket, ClientSession session)
        {
            //TODO: Add a penalty check when it will be ready
            const SayColorType type = SayColorType.White;
            await session.Character.MapInstance.SendPacket(session.Character.GenerateSay(new SayPacket
            {
                Message = clientSayPacket.Message,
                Type = type
            }), new EveryoneBut(session!.Channel!.Id)).ConfigureAwait(false); //TODO  ReceiverType.AllExceptMeAndBlacklisted
        }
    }
}