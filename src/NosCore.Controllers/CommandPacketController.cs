using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Packets.CommandPackets;

namespace NosCore.Controllers
{
    public class CommandPacketController : PacketController
    {
        public CommandPacketController()
        { }

        public void Speed(SpeedPacket speedPacket)
        {
            Session.Character.Speed = (speedPacket.Speed >= 60 ? (byte)59 : speedPacket.Speed);
            Session.SendPacket(Session.Character.GenerateCond());
        }
    }
}
