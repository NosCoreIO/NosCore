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

using Microsoft.AspNetCore.Mvc;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class StatController : Controller
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly WorldConfiguration _worldConfiguration;

        public StatController(WorldConfiguration worldConfiguration)
        {
            _worldConfiguration = worldConfiguration;
        }

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
                    session.SetLevel((byte) data.Data);
                    break;
                case UpdateStatActionType.UpdateJobLevel:
                    session.SetJobLevel((byte) data.Data);
                    break;
                case UpdateStatActionType.UpdateHeroLevel:
                    session.SetHeroLevel((byte) data.Data);
                    break;
                case UpdateStatActionType.UpdateReputation:
                    session.SetReputation(data.Data);
                    break;
                case UpdateStatActionType.UpdateGold:
                    if (session.Gold + data.Data > _worldConfiguration.MaxGoldAmount)
                    {
                        return BadRequest(); // MaxGold
                    }

                    session.SetGold(data.Data);
                    break;
                case UpdateStatActionType.UpdateClass:
                    session.ChangeClass((CharacterClassType) data.Data);
                    break;
                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.UNKWNOWN_RECEIVERTYPE));
                    break;
            }

            return Ok();
        }
    }
}