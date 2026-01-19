//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
