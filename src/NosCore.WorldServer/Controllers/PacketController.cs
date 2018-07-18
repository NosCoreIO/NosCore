using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.Serializing;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.Enumerations.Interaction;
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

            var message = PacketFactory.Deserialize(postedPacket.Packet);

            switch (postedPacket.ReceiverType)
            {
                case ReceiverType.All:
                    ServerManager.Instance.Broadcast(message);
                    break;
                case ReceiverType.OnlySomeone:
                    ClientSession receiverSession = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.Name == postedPacket.ReceiverCharacterData.CharacterName);

                    if (receiverSession == null)
                    {
                        return NotFound();
                    }

                    receiverSession.SendPacket(message);
                    break;
            }

            return Ok();

        }
    }
}
