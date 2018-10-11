using NosCore.Core.Handling;
using NosCore.Core.Networking;

namespace NosCore.GameObject.Networking
{
    public class PacketController : IPacketController
    {
        protected ClientSession Session { get; set; }

        public void RegisterSession(NetworkClient clientSession)
        {
            Session = (ClientSession) clientSession;
        }
    }
}