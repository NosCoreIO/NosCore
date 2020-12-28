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
using NosCore.Packets.Enumerations;
using Microsoft.AspNetCore.Mvc;
using NosCore.Core;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.BazaarService;
using NosCore.Shared.Enumerations;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.GameMaster)]
    public class BazaarController : Controller
    {
        private readonly IBazaarService _bazaarService;

        public BazaarController(IBazaarService bazaarService)
        {
            _bazaarService = bazaarService;
        }

        [HttpGet]
        public List<BazaarLink> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter) => _bazaarService.GetBazaar(id, index, pageSize, typeFilter,
            subTypeFilter, levelFilter, rareFilter, upgradeFilter, sellerFilter);


        [HttpDelete]
        public Task<bool> DeleteBazaarAsync(long id, short count, string requestCharacterName) => _bazaarService.DeleteBazaarAsync(id, count, requestCharacterName);

        [HttpPost]
        public Task<LanguageKey> AddBazaarAsync([FromBody] BazaarRequest bazaarRequest) => _bazaarService.AddBazaarAsync(bazaarRequest);

        [HttpPatch]
        public Task<BazaarLink?> ModifyBazaarAsync(long id, [FromBody] Json.Patch.JsonPatch bzMod) => _bazaarService.ModifyBazaarAsync(id, bzMod);
    }
}