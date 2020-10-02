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
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.PathFinder;
using NosCore.PathFinder.Interfaces;
using Serilog;

namespace NosCore.PacketHandlers.Movement
{
    public class WalkPacketHandler : PacketHandler<WalkPacket>, IWorldPacketHandler
    {
        private readonly IHeuristic _distanceCalculator;
        private readonly ILogger _logger;

        public WalkPacketHandler(IHeuristic distanceCalculator, ILogger logger)
        {
            _logger = logger;
            _distanceCalculator = distanceCalculator;
        }
        public override async Task ExecuteAsync(WalkPacket walkPacket, ClientSession session)
        {
            var distance = (int)_distanceCalculator.GetDistance((session.Character.PositionX, session.Character.PositionY), (walkPacket.XCoordinate, walkPacket.YCoordinate));

            if (((session.Character.Speed < walkPacket.Speed)
                && (session.Character.LastSpeedChange.AddSeconds(5) <= SystemTime.Now())) || (distance > 60))
            {
                return;
            }

            if ((walkPacket.XCoordinate + walkPacket.YCoordinate) % 3 % 2 != walkPacket.Unknown)
            {
                await session.DisconnectAsync();
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.WALK_CHECKSUM_INVALID), session.Character.VisualId);
                return;
            }

            var travelTime = 2500 / walkPacket.Speed * distance;
            if (travelTime > 1000)
            {
                await session.DisconnectAsync();
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SPEED_INVALID), session.Character.VisualId);
                return;
            }

            await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateMove(),
                new EveryoneBut(session.Channel!.Id)).ConfigureAwait(false);

            session.Character.LastMove = SystemTime.Now();
            if (session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
            {
                session.Character.MapX = walkPacket.XCoordinate;
                session.Character.MapY = walkPacket.YCoordinate;
            }

            session.Character.PositionX = walkPacket.XCoordinate;
            session.Character.PositionY = walkPacket.YCoordinate;
        }
    }
}