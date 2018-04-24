using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Packets.CommandPackets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            if (speedPacket != null)
            {
                if (speedPacket.Speed >= 60)
                {
                    return;
                }
                Session.Character.Speed = speedPacket.Speed;
                Session.SendPacket(Session.Character.GenerateCond());
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(speedPacket.Help(), 10));
            }
        }

        public void Help(HelpPacket helpPacket)
        {
            Session.SendPacket(Session.Character.GenerateSay("-------------Help command-------------", 11));

            List<Type> classes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(t => t.GetTypes()).Where(t => t.IsClass && t.Namespace == "NosCore.Packets.CommandPackets")
                .OrderBy(x => x.Name)
                .ToList();
            foreach (Type type in classes)
            {
                object classInstance = Activator.CreateInstance(type);
                Type classType = classInstance.GetType();
                MethodInfo method = classType.GetMethod("Help");
                if (method == null)
                {
                    continue;
                }

                string message = method.Invoke(classInstance, null).ToString();
                if (!string.IsNullOrEmpty(message))
                {
                    Session.SendPacket(Session.Character.GenerateSay(message, 12));
                }
            }
        }
        #endregion
    }
}