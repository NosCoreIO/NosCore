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
using NosCore.Packets.Enumerations;
using NosCore.Data.CommandPackets;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Command
{
    public class PositionPacketHandler : PacketHandler<PositionPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(PositionPacket _, ClientSession session)
        {
            session.SendPacketAsync(session.Character.GenerateSay(
                $"Map:{session.Character.MapInstance.Map.MapId} - X:{session.Character.PositionX} - Y:{session.Character.PositionY} - " +
                $"Dir:{session.Character.Direction} - Cell:{session.Character.MapInstance.Map[session.Character.PositionX, session.Character.PositionY]}",
                SayColorType.Green));
            return Task.CompletedTask;
        }
    }
}