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

using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using System.Collections.Generic;
using System.Threading.Tasks;
using Character = NosCore.GameObject.Character;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    public class ConnectedAccountController : Controller
    {
        private readonly Channel _channel;

        public ConnectedAccountController(Channel channel)
        {
            _channel = channel;
        }
        // GET api/connectedAccount
        [HttpGet]
        public List<ConnectedAccount> GetconnectedAccount()
        {
            return Broadcaster.Instance.ConnectedAccounts().Select(o =>
            {
                o.ChannelId = _channel.ChannelId;
                return o;
            }).ToList();
        }

        // DELETE api/connectedAccount
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