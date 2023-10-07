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
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    public class StatController(IOptions<WorldConfiguration> worldConfiguration, ILogger logger,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : Controller
    {
        // POST api/stat
        [HttpPost]
        public async Task<IActionResult> UpdateStatsAsync([FromBody] StatData data)
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
                    session.SetLevel((byte)data.Data);
                    break;
                case UpdateStatActionType.UpdateJobLevel:
                    await session.SetJobLevelAsync((byte)data.Data).ConfigureAwait(false);
                    break;
                case UpdateStatActionType.UpdateHeroLevel:
                    await session.SetHeroLevelAsync((byte)data.Data).ConfigureAwait(false);
                    break;
                case UpdateStatActionType.UpdateReputation:
                    await session.SetReputationAsync(data.Data).ConfigureAwait(false);
                    break;
                case UpdateStatActionType.UpdateGold:
                    if (session.Gold + data.Data > worldConfiguration.Value.MaxGoldAmount)
                    {
                        return BadRequest(); // MaxGold
                    }

                    await session.SetGoldAsync(data.Data).ConfigureAwait(false);
                    break;
                case UpdateStatActionType.UpdateClass:
                    await session.ChangeClassAsync((CharacterClassType)data.Data).ConfigureAwait(false);
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    break;
            }

            return Ok();
        }
    }
}