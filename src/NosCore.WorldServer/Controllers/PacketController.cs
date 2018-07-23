using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.Serializing;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.Enumerations.Interaction;

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
                    var receiverSession = ServerManager.Instance.Sessions.Values.FirstOrDefault(s =>
                        s.Character.Name == postedPacket.ReceiverCharacter.Name);

                    if (receiverSession == null)
                    {
                        return Ok();
                    }

                    receiverSession.SendPacket(message);
                    break;
            }

            return Ok();
        }
    }
}