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

using System;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Core;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.PathFinder;

namespace NosCore.PacketHandlers.Movement
{
    public class WalkPacketHandler : PacketHandler<WalkPacket>, IWorldPacketHandler
    {
        public override async Task Execute(WalkPacket walkPacket, ClientSession session)
        {
            var distance = (int) Heuristic.Octile(Math.Abs(session.Character.PositionX - walkPacket.XCoordinate),
                Math.Abs(session.Character.PositionY - walkPacket.YCoordinate));

            if (((session.Character.Speed < walkPacket.Speed)
                && (session.Character.LastSpeedChange.AddSeconds(5) <= SystemTime.Now())) || (distance > 60))
            {
                return;
            }

            //todo check speed and distance
            if ((walkPacket.XCoordinate + walkPacket.YCoordinate) % 3 % 2 != walkPacket.Unknown)
            {
                //todo log and disconnect
                return;
            }

            if (session.Character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance)
            {
                session.Character.MapX = walkPacket.XCoordinate;
                session.Character.MapY = walkPacket.YCoordinate;
            }

            session.Character.PositionX = walkPacket.XCoordinate;
            session.Character.PositionY = walkPacket.YCoordinate;

            await session.Character.MapInstance?.SendPacket(session.Character.GenerateMove(),
                new EveryoneBut(session.Channel.Id));
            session.Character.LastMove = SystemTime.Now();
        }
    }
}