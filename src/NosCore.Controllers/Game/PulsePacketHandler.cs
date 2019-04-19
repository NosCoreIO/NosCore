using ChickenAPI.Packets.ClientPackets.Movement;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Game
{
    public class PulsePacketHandler : PacketHandler<PulsePacket>, IWorldPacketHandler
    {
        public override void Execute(PulsePacket pulsePacket, ClientSession session)
        {
            session.LastPulse += 60;
            if (pulsePacket.Tick != session.LastPulse)
            {
                session.Disconnect();
            }
        }
    }
}
