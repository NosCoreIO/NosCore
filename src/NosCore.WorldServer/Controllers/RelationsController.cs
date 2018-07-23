using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class RelationsController : Controller
    {
        // POST api/relations
        [HttpPost]
        public IActionResult PostUpdateFriendList([FromBody] PostedPacket postedPacket)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var session = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.CharacterId == postedPacket.ReceiverCharacterData.CharacterId);

            session?.SendPacket(session.Character.GenerateFinfo(postedPacket.SenderCharacterData.CharacterId, postedPacket.SenderCharacterData.RelationData.IsConnected ));

            return Ok();
        }

        // POST api/relations/deleteRelation
        [HttpPost("deleteRelation")]
        public IActionResult DeleteRelation([FromBody] PostedPacket postedPacket)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var session = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.CharacterId == postedPacket.ReceiverCharacterData.CharacterId);

            if (session == null)
            {
                return Ok();
            }

            session.Character.CharacterRelations.TryRemove(postedPacket.ReceiverCharacterData.RelationData.RelationId, out _);
            session.SendPacket(session.Character.GenerateFinit());

            return Ok();
        }
    }
}
