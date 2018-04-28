using NosCore.Core;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;

namespace NosCore.Controllers
{
    public class UselessPacketController : PacketController
    {
        #region Members

        private readonly ClientSession  _session;

        #endregion

        #region Instantiation
        public UselessPacketController()
        { }
        public UselessPacketController(ClientSession  session)
        {
            _session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session => _session;

        #endregion

        #region Methods

        public void CClose(CClosePacket packet)
        {
            // idk
        }

        public void FStashEnd(FStashEndPacket packet)
        {
            // idk
        }

        #endregion
    }

}
