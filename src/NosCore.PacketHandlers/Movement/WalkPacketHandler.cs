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

using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.PathFinder.Interfaces;
using Serilog;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Core.Networking.ChannelMatcher;

namespace NosCore.PacketHandlers.Movement
{
    public class WalkPacketHandler : PacketHandler<WalkPacket>, IWorldPacketHandler
    {
        private readonly IHeuristic _distanceCalculator;
        private readonly ILogger _logger;
        // this is used to avoid network issue to be counted as speed hack.
        private readonly int _speedDiffAllowed = 1 / 3;
        private readonly IClock _clock;

        public WalkPacketHandler(IHeuristic distanceCalculator, ILogger logger, IClock clock)
        {
            _logger = logger;
            _distanceCalculator = distanceCalculator;
            _clock = clock;
        }
        public override async Task ExecuteAsync(WalkPacket walkPacket, ClientSession session)
        {
            var distance = (int)_distanceCalculator.GetDistance((session.Character.PositionX, session.Character.PositionY), (walkPacket.XCoordinate, walkPacket.YCoordinate)) - 1;

            if (((session.Character.Speed < walkPacket.Speed)
                && (session.Character.LastSpeedChange.Plus(Duration.FromSeconds(5)) <= _clock.GetCurrentInstant())) || (distance > session.Character.Speed / 2))
            {
                return;
            }

            if ((walkPacket.XCoordinate + walkPacket.YCoordinate) % 3 % 2 != walkPacket.CheckSum)
            {
                await session.DisconnectAsync();
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.WALK_CHECKSUM_INVALID), session.Character.VisualId);
                return;
            }

            var travelTime = 2500 / walkPacket.Speed * distance;
            if (travelTime > 1000 * (_speedDiffAllowed + 1))
            {
                await session.DisconnectAsync();
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SPEED_INVALID), session.Character.VisualId);
                return;
            }

            await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateMove(),
                new EveryoneBut(session.Channel!.Id)).ConfigureAwait(false);

            session.Character.LastMove = _clock.GetCurrentInstant();
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
