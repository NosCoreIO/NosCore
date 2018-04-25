using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Packets.CommandPackets;

namespace NosCore.Handler
{
    public class CommandPacketHandler
    {
        #region Members

        #endregion

        #region Instantiation
        public CommandPacketHandler()
        { }
        public CommandPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods
        public void Speed(SpeedPacket speedPacket)
        {
            if (speedPacket.Speed >= 60)
            {
                Session.Character.Speed = 59;
                return;
            }
            Session.Character.Speed = speedPacket.Speed;
            Session.SendPacket(Session.Character.GenerateCond());
        }
        #endregion
    }
}
