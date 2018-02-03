using OpenNosCore.Core.Serializing;
using OpenNosCore.GameObject;
using OpenNosCore.Packets.ClientPackets;

namespace OpenNosCore.GameHandler
{
    public class DefaultPacketHandler : IPacketHandler
    {
        #region Members

        private readonly ClientSession  _session;

        #endregion

        #region Instantiation
        public DefaultPacketHandler()
        { }
        public DefaultPacketHandler(ClientSession  session)
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

        public void GameStart(GameStartPacket packet)
        {
            Session.ChangeMap();
        }

        #endregion
    }

}
