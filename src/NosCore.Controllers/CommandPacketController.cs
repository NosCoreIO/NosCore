using NosCore.Core;
using NosCore.Core.Extensions;
using NosCore.Core.Serializing;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Packets.CommandPackets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NosCore.Controllers
{
    public class CommandPacketController : PacketController
    {
        public CommandPacketController()
        { }

        public void Speed(SpeedPacket speedPacket)
        {
            if (speedPacket.Speed > 0 && speedPacket.Speed < 60)
            {
                Session.Character.Speed = (speedPacket.Speed >= 60 ? (byte)59 : speedPacket.Speed);
                Session.SendPacket(Session.Character.GenerateCond());
            }
            else
            {
                Session.SendPacket(Session.Character.GenerateSay(speedPacket.Help(), Shared.SayColorType.Yellow));
            }
        }

        public void Help(HelpPacket helpPacket)
        {
            Session.SendPacket(Session.Character.GenerateSay("-------------Help command-------------", Shared.SayColorType.Purple));
            List<Type> classes = helpPacket.GetType().Assembly.GetTypes().Where(t => typeof(ICommandPacket).IsAssignableFrom(t) && t.GetCustomAttribute<PacketHeaderAttribute>()?.Authority <= Session.Account.Authority).OrderBy(x => x.Name).ToList();
            foreach (Type type in classes)
            {
                ICommandPacket classInstance = type.CreateInstance<ICommandPacket>();
                MethodInfo method = type.GetMethod("Help");
                if (method == null)
                {
                    continue;
                }
               
                string message = method.Invoke(classInstance, null).ToString();
                if (!string.IsNullOrEmpty(message))
                {
                    Session.SendPacket(Session.Character.GenerateSay(message, Shared.SayColorType.Green));
                }
            }
        }
    }
}
