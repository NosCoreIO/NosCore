using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class StatController : Controller
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        // POST api/stat
        [HttpPost]
        public IActionResult UpdateStats([FromBody] StatData data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var session = Broadcaster.Instance.GetCharacter(s => s.Name == data.Character?.Name);

            if (session == null)
            {
                return Ok(); //TODO: not found
            }

            switch (data.ActionType)
            {
                case UpdateStatActionType.UpdateLevel:
                    session.SetLevel(data.Data);
                    break;
                case UpdateStatActionType.UpdateJobLevel:
                    session.SetJobLevel(data.Data);
                    break;
                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.UNKWNOWN_RECEIVERTYPE));
                    break;
            }

            return Ok();
        }
    }
}
