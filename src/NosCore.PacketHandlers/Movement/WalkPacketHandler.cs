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

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Systems;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.PathFinder.Interfaces;
using Serilog;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Shared.I18N;
using NosCore.GameObject.Networking;

namespace NosCore.PacketHandlers.Movement
{
    public class WalkPacketHandler(IHeuristic distanceCalculator, ILogger logger, IClock clock,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IEntityPacketSystem entityPacketSystem)
        : PacketHandler<WalkPacket>, IWorldPacketHandler
    {
        // this is used to avoid network issue to be counted as speed hack.
        private readonly double _speedDiffAllowed = 1D / 3;

        public override async Task ExecuteAsync(WalkPacket walkPacket, ClientSession session)
        {
            var distance = (int)distanceCalculator.GetDistance((session.Player.PositionX, session.Player.PositionY), (walkPacket.XCoordinate, walkPacket.YCoordinate)) - 1;

            var characterSpeed = session.Player.Speed;
            if ((characterSpeed < walkPacket.Speed) || (distance > characterSpeed / 2))
            {
                return;
            }

            if ((walkPacket.XCoordinate + walkPacket.YCoordinate) % 3 % 2 != walkPacket.CheckSum)
            {
                await session.DisconnectAsync();
                logger.Error(logLanguage[LogLanguageKey.WALK_CHECKSUM_INVALID], session.Player.VisualId);
                return;
            }

            var travelTime = 2500 / walkPacket.Speed * distance;
            if (travelTime > 1000 * (_speedDiffAllowed + 1))
            {
                await session.DisconnectAsync();
                logger.Error(logLanguage[LogLanguageKey.SPEED_INVALID], session.Player.VisualId);
                return;
            }

            await session.Player.MapInstance.SendPacketAsync(entityPacketSystem.GenerateMove(session.Player),
                new EveryoneBut(session.Channel!.Id)).ConfigureAwait(false);

            var player = session.Player;
            player.LastMove = clock.GetCurrentInstant();
            if (session.Player.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
            {
                player.MapX = walkPacket.XCoordinate;
                player.MapY = walkPacket.YCoordinate;
            }

            player.SetPosition(walkPacket.XCoordinate, walkPacket.YCoordinate);
        }
    }
}
