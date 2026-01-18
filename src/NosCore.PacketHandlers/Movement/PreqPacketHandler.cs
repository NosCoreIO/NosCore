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

using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.PathFinder.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.Networking;
using NosCore.Packets.Enumerations;

namespace NosCore.PacketHandlers.Movement
{
    public class PreqPacketHandler(IMapInstanceAccessorService mapInstanceAccessorService,
            IHeuristic distanceCalculator, IClock clock, IMapChangeService mapChangeService)
        : PacketHandler<PreqPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PreqPacket _, ClientSession session)
        {
            if (((clock.GetCurrentInstant() - session.Player.LastPortal).TotalSeconds < 4) ||
                (session.Player.LastPortal > session.Player.LastMove))
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Player.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.WillMoveShortly
                }).ConfigureAwait(false);
                return;
            }

            var world = session.Player.MapInstance.EcsWorld;
            var characterId = session.Player.CharacterId;
            var portalData = session.Player.MapInstance.Portals
                .Select(e => (Entity: e, Portal: e.GetPortal(world)))
                .Where(p => p.Portal != null && (p.Portal.Value.OwnerId == null || p.Portal.Value.OwnerId == characterId))
                .FirstOrDefault(p =>
                    distanceCalculator.GetDistance(
                        (session.Player.PositionX, session.Player.PositionY),
                        (p.Portal!.Value.SourceX, p.Portal.Value.SourceY)) <= 2);

            if (portalData.Portal == null)
            {
                return;
            }

            var portal = portalData.Portal.Value;
            if (portal.DestinationMapInstanceId == default)
            {
                return;
            }

            var player = session.Player;
            player.LastPortal = clock.GetCurrentInstant();

            if ((mapInstanceAccessorService.GetMapInstance(portal.SourceMapInstanceId)!.MapInstanceType
                    != MapInstanceType.BaseMapInstance)
                && (mapInstanceAccessorService.GetMapInstance(portal.DestinationMapInstanceId)!.MapInstanceType
                    == MapInstanceType.BaseMapInstance))
            {
                await mapChangeService.ChangeMapAsync(session, session.Player.MapId, session.Player.MapX, session.Player.MapY).ConfigureAwait(false);
            }
            else
            {
                await mapChangeService.ChangeMapInstanceAsync(session, portal.DestinationMapInstanceId, portal.DestinationX,
                    portal.DestinationY).ConfigureAwait(false);
            }
        }
    }
}