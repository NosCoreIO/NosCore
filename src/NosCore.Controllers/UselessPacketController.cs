using JetBrains.Annotations;
using NosCore.GameObject;
using NosCore.Packets.ClientPackets;

namespace NosCore.Controllers
{
    public class UselessPacketController : PacketController
    {
        public void CClose([UsedImplicitly] CClosePacket cClosePacket)
        {
            // idk
        }

        public void FStashEnd([UsedImplicitly] FStashEndPacket fStashEndPacket)
        {
            // idk
        }
    }
}