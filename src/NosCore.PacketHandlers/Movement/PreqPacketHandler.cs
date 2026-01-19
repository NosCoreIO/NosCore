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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.Enumerations;
using NosCore.PathFinder.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.PacketHandlers.Movement
{
    public class PreqPacketHandler(IMapInstanceAccessorService mapInstanceAccessorService,
            IMinilandService minilandProvider, IHeuristic distanceCalculator, IClock clock,
            IMapChangeService mapChangeService)
        : PacketHandler<PreqPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(PreqPacket _, ClientSession session)
        {
            if (((clock.GetCurrentInstant() - session.Character.LastPortal).TotalSeconds < 4) ||
                (session.Character.LastPortal > session.Character.LastMove))
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.WillMoveShortly
                });
                return;
            }

            var portals = new List<Portal>();
            portals.AddRange(session.Character.MapInstance.Portals);
            portals.AddRange(minilandProvider
                .GetMinilandPortals(session.Character.CharacterId)
                .Where(s => s.SourceMapInstanceId == session.Character.MapInstanceId));
            var portal = portals.Find(port =>
                distanceCalculator.GetDistance((session.Character.PositionX, session.Character.PositionY), (port.SourceX, port.SourceY))
                  <= 2);
            if (portal == null)
            {
                return;
            }

            if (portal.DestinationMapInstanceId == default)
            {
                return;
            }

            session.Character.LastPortal = clock.GetCurrentInstant();

            if ((mapInstanceAccessorService.GetMapInstance(portal.SourceMapInstanceId)!.MapInstanceType
                    != MapInstanceType.BaseMapInstance)
                && (mapInstanceAccessorService.GetMapInstance(portal.DestinationMapInstanceId)!.MapInstanceType
                    == MapInstanceType.BaseMapInstance))
            {
                await mapChangeService.ChangeMapAsync(session, session.Character.MapId, session.Character.MapX, session.Character.MapY);
            }
            else
            {
                await mapChangeService.ChangeMapInstanceAsync(session, portal.DestinationMapInstanceId, portal.DestinationX,
                    portal.DestinationY);
            }
        }
    }
}