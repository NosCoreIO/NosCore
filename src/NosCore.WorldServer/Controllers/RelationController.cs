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
    public class RelationController : Controller
    {
        // DELETE api/relation
        [HttpDelete]
        public IActionResult DeleteRelation(long id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var session = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character?.CharacterRelations.Any(r => r.Key == id) == true);

            if (session == null)
            {
                return Ok();
            }

            session.Character.CharacterRelations.TryRemove(id, out _);
            session.SendPacket(session.Character.GenerateFinit());

            return Ok();
        }
    }
}
