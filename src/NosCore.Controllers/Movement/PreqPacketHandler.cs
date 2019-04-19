using System;
using ChickenAPI.Packets.ClientPackets.Movement;
using ChickenAPI.Packets.Enumerations;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.PathFinder;

namespace NosCore.PacketHandlers.Movement
{
    public class PreqPacketHandler : PacketHandler<PreqPacket>, IWorldPacketHandler
    {
        private readonly IMapInstanceProvider _mapInstanceProvider;
        public PreqPacketHandler(IMapInstanceProvider mapInstanceProvider)
        {
            _mapInstanceProvider = mapInstanceProvider;
        }

        public override void Execute(PreqPacket _, ClientSession session)
        {
            if ((SystemTime.Now() - session.Character.LastPortal).TotalSeconds < 4 ||
                session.Character.LastPortal > session.Character.LastMove)
            {
                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.PORTAL_DELAY, session.Account.Language), SayColorType.Yellow));
                return;
            }

            var portal = session.Character.MapInstance.Portals.Find(port =>
                Heuristic.Octile(Math.Abs(session.Character.PositionX - port.SourceX),
                    Math.Abs(session.Character.PositionY - port.SourceY)) <= 2);
            if (portal == null) 
            {
                return;
            }

            if (portal.DestinationMapInstanceId == default)
            {
                return;
            }

            session.Character.LastPortal = SystemTime.Now();

            if (_mapInstanceProvider.GetMapInstance(portal.SourceMapInstanceId).MapInstanceType
                != MapInstanceType.BaseMapInstance
                && _mapInstanceProvider.GetMapInstance(portal.DestinationMapInstanceId).MapInstanceType
                == MapInstanceType.BaseMapInstance)
            {
                session.ChangeMap(session.Character.MapId, session.Character.MapX, session.Character.MapY);
            }
            else
            {
                session.ChangeMapInstance(portal.DestinationMapInstanceId, portal.DestinationX,
                    portal.DestinationY);
            }
        }
    }
}
