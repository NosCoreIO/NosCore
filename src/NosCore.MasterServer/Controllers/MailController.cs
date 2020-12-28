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

using System.Collections.Generic;
using System.Threading.Tasks;
using Json.Patch;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Data.WebApi;
using NosCore.GameObject.Providers.MailService;
using NosCore.Shared.Enumerations;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class MailController : Controller
    {
        private readonly IMailService _mailService;

        public MailController(IMailService mailService)
        {
            _mailService = mailService;
        }

        [HttpGet]
        public List<MailData> GetMails(long id, long characterId, bool senderCopy) => _mailService.GetMails(id, characterId, senderCopy);

        [HttpDelete]
        public Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy) => _mailService.DeleteMailAsync(id, characterId, senderCopy);

        [HttpPatch]
        public Task<MailData?> ViewMailAsync(long id, [FromBody] JsonPatch mailData) => _mailService.EditMailAsync(id, mailData);

        [HttpPost]
        public Task<bool> SendMailAsync([FromBody] MailRequest mail) => _mailService.SendMailAsync(mail);
    }
}