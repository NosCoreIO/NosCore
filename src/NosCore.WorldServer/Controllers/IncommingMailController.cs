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
using System.Threading.Tasks;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Parcel;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Shared.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;

namespace NosCore.WorldServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class IncommingMailController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> IncommingMailAsync([FromBody] MailData data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var session = Broadcaster.Instance.GetCharacter(s => s.Name == data.ReceiverName);

            if (session == null)
            {
                throw new InvalidOperationException();
            }

            if (data.ItemInstance != null)
            {
                await session.SendPacketAsync(session.GenerateSay(
                    string.Format(GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_GIFTED, session.AccountLanguage),
                        data.ItemInstance.Amount), SayColorType.Green)).ConfigureAwait(false);
            }

            await session.GenerateMailAsync(new[] {data}).ConfigureAwait(false);
            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteMailAsync(long id, short mailId, byte postType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var session = Broadcaster.Instance.GetCharacter(s => s.VisualId == id);

            if (session == null)
            {
                throw new InvalidOperationException();
            }

            await session.SendPacketAsync(new PostPacket
            {
                Type = 2,
                PostType = postType,
                Id = mailId
            }).ConfigureAwait(false);
            return Ok();
        }
    }
}