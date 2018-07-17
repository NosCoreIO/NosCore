using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NosCore.Core.Extensions;
using NosCore.Core.Networking;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Packets.CommandPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;

namespace NosCore.Controllers
{
	[UsedImplicitly]
    public class CommandPacketController : PacketController
	{
	    public void Shout(ShoutPacket shoutPacket)
	    {
	        if (shoutPacket?.Message == null)
	        {
	            return;
	        }

	        var sayPacket = new SayPacket
	        {
                VisualType = VisualType.Player,
                VisualId = 0,
                Type = SayColorType.Yellow,
                Message = $"({Language.Instance.GetMessageFromKey(LanguageKey.ADMINISTRATOR, Session.Account.Language)}){shoutPacket.Message}"
	        };

            var msgPacket = new MsgPacket
            {
                Type = 2,
                Message = shoutPacket.Message
            };

	        var sayPostedPacket = new PostedPacket
	        {
                Packet = PacketFactory.Serialize(sayPacket),
                SenderCharacterData = new CharacterData { CharacterName = Session.Character.Name, CharacterId = Session.Character.CharacterId },
                MessageType = MessageType.Shout,
                PacketHeader = typeof(SayPacket)
	        };

            var msgPostedPacket = new PostedPacket
            {
                Packet = PacketFactory.Serialize(msgPacket),
                MessageType = MessageType.Shout,
                PacketHeader = typeof(MsgPacket)
            };

            ServerManager.Instance.BroadcastPackets(new List<PostedPacket>(new[] {sayPostedPacket, msgPostedPacket}));
	    }

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