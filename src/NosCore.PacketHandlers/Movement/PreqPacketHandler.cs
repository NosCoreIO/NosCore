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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.Enumerations;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MinilandProvider;
using NosCore.PathFinder;
using NosCore.PathFinder.Interfaces;

namespace NosCore.PacketHandlers.Movement
{
    public class PreqPacketHandler : PacketHandler<PreqPacket>, IWorldPacketHandler
    {
        private readonly IMapInstanceProvider _mapInstanceProvider;
        private readonly IMinilandProvider _minilandProvider;
        private readonly IHeuristic _distanceCalculator;

        public PreqPacketHandler(IMapInstanceProvider mapInstanceProvider, IMinilandProvider minilandProvider, IHeuristic distanceCalculator)
        {
            _mapInstanceProvider = mapInstanceProvider;
            _minilandProvider = minilandProvider;
            _distanceCalculator = distanceCalculator;
        }

        public override async Task ExecuteAsync(PreqPacket _, ClientSession session)
        {
            if (((SystemTime.Now() - session.Character.LastPortal).TotalSeconds < 4) ||
                (session.Character.LastPortal > session.Character.LastMove))
            {
                await session.SendPacketAsync(session.Character.GenerateSay(
                    GameLanguage.Instance.GetMessageFromKey(LanguageKey.PORTAL_DELAY, session.Account.Language),
                    SayColorType.Yellow)).ConfigureAwait(false);
                return;
            }

            var portals = new List<Portal>();
            portals.AddRange(session.Character.MapInstance.Portals);
            portals.AddRange(_minilandProvider
                .GetMinilandPortals(session.Character.CharacterId)
                .Where(s => s.SourceMapInstanceId == session.Character.MapInstanceId));
            var portal = portals.Find(port =>
                _distanceCalculator.GetDistance((session.Character.PositionX, session.Character.PositionY), (port.SourceX, port.SourceY))
                  <= 2);
            if (portal == null)
            {
                return;
            }

            if (portal.DestinationMapInstanceId == default)
            {
                return;
            }

            session.Character.LastPortal = SystemTime.Now();

            if ((_mapInstanceProvider.GetMapInstance(portal.SourceMapInstanceId)!.MapInstanceType
                    != MapInstanceType.BaseMapInstance)
                && (_mapInstanceProvider.GetMapInstance(portal.DestinationMapInstanceId)!.MapInstanceType
                    == MapInstanceType.BaseMapInstance))
            {
                await session.ChangeMapAsync(session.Character.MapId, session.Character.MapX, session.Character.MapY).ConfigureAwait(false);
            }
            else
            {
                await session.ChangeMapInstanceAsync(portal.DestinationMapInstanceId, portal.DestinationX,
                    portal.DestinationY).ConfigureAwait(false);
            }
        }
    }
}