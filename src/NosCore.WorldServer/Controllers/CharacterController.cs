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

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class CharacterController : Controller
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        // POST api/character
        [HttpPost]
        public IActionResult Disconnect([FromBody] Character character)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var targetSession = Broadcaster.Instance.GetCharacter(s => s.Name == character.Name);
            if (targetSession == null)
            {
                _logger.Information(targetSession.GetMessageFromKey(LanguageKey.USER_NOT_CONNECTED));
                return Ok(); //TODO: not found
            }

            targetSession.Channel.DisconnectAsync(); // TODO: Find a better way to do it
            return Ok();
        }
    }
}