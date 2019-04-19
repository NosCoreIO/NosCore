using System;
using ChickenAPI.Packets.ClientPackets.Movement;
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
        public override void Execute(WalkPacket walkPacket, ClientSession session)
        {
            var distance = (int)Heuristic.Octile(Math.Abs(session.Character.PositionX - walkPacket.XCoordinate),
                Math.Abs(session.Character.PositionY - walkPacket.YCoordinate));

            if ((session.Character.Speed < walkPacket.Speed
                && session.Character.LastSpeedChange.AddSeconds(5) <= SystemTime.Now()) || distance > 60)
            {
                return;
            }

            if (session.Character.MapInstance?.MapInstanceType == MapInstanceType.BaseMapInstance)
            {
                session.Character.MapX = walkPacket.XCoordinate;
                session.Character.MapY = walkPacket.YCoordinate;
            }

            session.Character.PositionX = walkPacket.XCoordinate;
            session.Character.PositionY = walkPacket.YCoordinate;

            session.Character.MapInstance?.Sessions.SendPacket(session.Character.GenerateMove(),
                new EveryoneBut(session.Channel.Id));
            session.Character.LastMove = SystemTime.Now();
        }
    }
}
