using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NosCore.Core.Extensions;
using NosCore.Core.Serializing;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Packets.CommandPackets;
using NosCore.Shared.Enumerations;

namespace NosCore.Controllers
{
	[UsedImplicitly]
    public class CommandPacketController : PacketController
	{
		[UsedImplicitly]
        public void Speed(SpeedPacket speedPacket)
		{
			if (speedPacket.Speed > 0 && speedPacket.Speed < 60)
			{
				Session.Character.Speed = speedPacket.Speed >= 60 ? (byte) 59 : speedPacket.Speed;
				Session.SendPacket(Session.Character.GenerateCond());
			}
			else
			{
				Session.SendPacket(Session.Character.GenerateSay(speedPacket.Help(), SayColorType.Yellow));
			}
		}

        [UsedImplicitly]
        public void ChangeRep(ClientSession session, long reput)
        {
            if(session == null || reput < 0)
            {
                return;
            }

            Session.Character.Reput = reput;
            Session.SendPacket(Session.Character.GenerateFd());
        }

		[UsedImplicitly]
        public void Help(HelpPacket helpPacket)
		{
			Session.SendPacket(Session.Character.GenerateSay("-------------Help command-------------",
				SayColorType.Purple));
			var classes = helpPacket.GetType().Assembly.GetTypes().Where(t =>
                    typeof(ICommandPacket).IsAssignableFrom(t)
                    && t.GetCustomAttribute<PacketHeaderAttribute>()?.Authority <= Session.Account.Authority)
				.OrderBy(x => x.Name).ToList();
			foreach (var type in classes)
			{
				var classInstance = type.CreateInstance<ICommandPacket>();
				var method = type.GetMethod("Help");
				if (method == null)
				{
					continue;
				}

				var message = method.Invoke(classInstance, null).ToString();
				if (!string.IsNullOrEmpty(message))
				{
					Session.SendPacket(Session.Character.GenerateSay(message, SayColorType.Green));
				}
			}
		}
	}
}