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

using NosCore.Data.CommandPackets;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;
using NosCore.Core.Networking;

namespace NosCore.PacketHandlers.Command
{
    public class InvisibleCommandPacketHandler : PacketHandler<InvisibleCommandPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(InvisibleCommandPacket changeClassPacket, ClientSession session)
        {
            session.Character.Camouflage = !session.Character.Camouflage;
            session.Character.Invisible = !session.Character.Invisible;
            return session.Character.MapInstance.SendPacketAsync(session.Character.GenerateInvisible());
            //Session.SendPacket(Session.Character.GenerateEq());
        }
    }
}