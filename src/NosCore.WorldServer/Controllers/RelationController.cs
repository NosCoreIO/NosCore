//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class RelationController : Controller
    {
        // DELETE api/relation
        [HttpDelete]
        public IActionResult DeleteRelation(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var session = Broadcaster.Instance.GetCharacter(s =>
                s.CharacterRelations.Any(r => r.Key == id));

            if (session == null)
            {
                return Ok(); //TODO: not found
            }

            session.CharacterRelations.TryRemove(id, out var relation);
            session.CharacterRelations.TryRemove(
                session.RelationWithCharacter.Values.First(s => s.RelatedCharacterId == relation.CharacterId)
                    .CharacterRelationId, out _);

            session.SendPacket(session.GenerateFinit());

            return Ok(relation);
        }
    }
}