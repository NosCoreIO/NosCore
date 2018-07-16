using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    public class PacketController : Controller
    {
        // POST api/packet
        [HttpPost]
        [AllowAnonymous]
        public void SendMessageToCharacter(string postedData)
        {
            var postedPacket = JsonConvert.DeserializeObject<PostedPacket>(postedData);
            ClientSession senderSession = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.Name == postedPacket.Sender);
            ClientSession receiverSession = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.Name == postedPacket.Receiver);

            if (receiverSession == null)
            {
                return;
            }

            if (senderSession == null)
            {
                postedPacket.Packet += $" <{Language.Instance.GetMessageFromKey(LanguageKey.CHANNEL, receiverSession.Account.Language)}: {postedPacket.SenderWorldId}";
            }

            receiverSession.SendPacket(postedPacket.Packet);
        }

    }
}
