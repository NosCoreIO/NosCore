using OpenNosCore.Core.Serializing;
using OpenNosCore.GameObject;
using OpenNosCore.Packets.ClientPackets;

namespace OpenNosCore.GameHandler
{
    public class UselessPacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession  _session;

        #endregion

        #region Instantiation
        public UselessPacketHandler()
        { }
        public UselessPacketHandler(ClientSession  session)
        {
            _session = session;
        }

        #endregion

        #region Properties

        public ClientSession  Session
        {
            get
            {
                return _session;
            }
        }

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
