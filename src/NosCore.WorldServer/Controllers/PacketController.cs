using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.WebApiData;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    public class PacketController : Controller
    {
        // POST api/packet
        [HttpPost]
        [AllowAnonymous]
        public void SendPacketToCharacter(string postedData)
        {
            var postedPacket = JsonConvert.DeserializeObject<PostedPacket>(postedData);
            ClientSession receiverSession = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.Name == postedPacket.Receiver);

            receiverSession?.SendPacket(postedPacket.Packet);
        }

    }
}
