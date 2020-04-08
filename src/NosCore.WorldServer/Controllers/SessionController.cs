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

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Data.Enumerations.Account;
using NosCore.GameObject;
using NosCore.GameObject.Networking;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class SessionController : Controller
    {
        // DELETE api/session
        [HttpDelete]
        public async Task<IActionResult> DisconnectAsync(long id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var targetSession = Broadcaster.Instance.GetCharacter(s => s.VisualId == id) as Character;
            if (targetSession?.Session == null)
            {
                return Ok(); // TODO : Handle 404 in WebApi
            }

            await targetSession.Session.DisconnectAsync().ConfigureAwait(false);
            return Ok();
        }
    }
}