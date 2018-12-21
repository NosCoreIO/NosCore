//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class SessionController : Controller
    {
        // DELETE api/session
        [HttpDelete]
        public IActionResult Disconnect(long id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            Character targetSession = (Character)Broadcaster.Instance.GetCharacter(s => s.VisualId == id);
            if (targetSession.Session == null)
            {
                return Ok(); // TODO : Handle 404 in WebApi
            }

            targetSession.Session.Disconnect();
            Broadcaster.Instance.UnregisterSession(targetSession.Session);
            return Ok();
        }
    }
}