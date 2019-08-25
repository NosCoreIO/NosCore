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

using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.Movement;
using Microsoft.AspNetCore.Mvc;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using Serilog;
using System;
using ServiceStack;

namespace NosCore.WorldServer.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class IncommingMailController : Controller
    {
        [HttpPost]
        public IActionResult IncommingMail([FromBody] MailData data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var session = Broadcaster.Instance.GetCharacter(s => s.Name == data.CharacterName);

            if (session == null)
            {
                throw new InvalidOperationException();
            }

            session.SendPacket(session.GenerateSay(
                string.Format(Language.Instance.GetMessageFromKey(LanguageKey.ITEM_GIFTED, session.AccountLanguage), data.Amount), SayColorType.Green));
            session.SendPacket(new ParcelPacket
            {
                Type = 1,
                Unknown = 1,
                Id = data.MailId,
                ParcelAttachment = new ParcelAttachmentSubPacket
                {
                    TitleType = data.Title == "NOSMALL" ? (byte)1 : (byte)4,
                    Unknown2 = 0,
                    Date = data.Date.ToString("yyMMddHHmm"),
                    Title = data.Title,
                    AttachmentVNum = data.AttachmentVNum,
                    AttachmentAmount = data.Amount,
                    ItemType = data.ItemType
                }
            });

            return Ok();
        }
    }
}