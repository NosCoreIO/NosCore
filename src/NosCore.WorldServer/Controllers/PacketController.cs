using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.Serializing;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class PacketController : Controller
    {
        // POST api/packet
        [HttpPost]
        public IActionResult PostPacket([FromBody] PostedPacket postedPacket)
        {
	        if (!ModelState.IsValid)
	        {
		        return BadRequest();
	        }

	        switch (postedPacket.MessageType)
	        {
		        case MessageType.Shout:
			        ServerManager.Instance.Broadcast(PacketFactory.Deserialize(postedPacket.Packet, postedPacket.PacketHeader));
			        break;

		        case MessageType.Whisper:
		        case MessageType.WhisperGm:
		        case MessageType.PrivateChat:
			        ClientSession senderSession =
				        ServerManager.Instance.Sessions.Values.FirstOrDefault(s =>
					        s.Character.Name == postedPacket.SenderCharacterData.CharacterName);
			        ClientSession receiverSession =
				        ServerManager.Instance.Sessions.Values.FirstOrDefault(s =>
					        s.Character.Name == postedPacket.ReceiverCharacterData.CharacterName);

			        if (receiverSession == null)
			        {
				        return Ok();
			        }

			        if (senderSession == null)
			        {
				       postedPacket.Packet += $" <{Language.Instance.GetMessageFromKey(LanguageKey.CHANNEL, receiverSession.Account.Language)}: {postedPacket.OriginWorldId}";
			        }

			        receiverSession.SendPacket(PacketFactory.Deserialize(postedPacket.Packet, postedPacket.PacketHeader));
			        break;
		        case MessageType.Family:
			        break;
		        case MessageType.FamilyChat:
			        break;
	        }

	        return Ok();

        }
    }
}
