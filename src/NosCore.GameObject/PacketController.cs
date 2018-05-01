using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.GameObject
{
    public class PacketController : IPacketController
    {
        protected ClientSession Session { get; set; }

        public void RegisterSession(NetworkClient clientSession)
        {
            Session = (ClientSession)clientSession;
        }
    }
}
