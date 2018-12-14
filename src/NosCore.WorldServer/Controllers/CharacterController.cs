using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NosCore.Core;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations.Account;
using Serilog;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.Moderator)]
    public class CharacterController : Controller
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        // POST api/disconnect
        public IActionResult Disconnect([FromBody] Character character)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            ICharacterEntity targetSession = Broadcaster.Instance.GetCharacter(s => s.Name == character.Name);

            if (targetSession == null)
            {
                // PLAYER NOT FOUND
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.USER_NOT_CONNECTED));
                return null;
            }

            targetSession.Channel.DisconnectAsync();
            return Ok();
        }
    }
}
