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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NosCore.Core;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Services.BazaarService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub
{
    public class BazaarHub(IBazaarService bazaarService) : Hub, IBazaarHub
    {
        public Task<List<BazaarLink>> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter) => Task.FromResult(bazaarService.GetBazaar(id, index, pageSize, typeFilter,
            subTypeFilter, levelFilter, rareFilter, upgradeFilter, sellerFilter));

        public Task<bool> DeleteBazaarAsync(long id, short count, string requestCharacterName) => bazaarService.DeleteBazaarAsync(id, count, requestCharacterName);

        public Task<LanguageKey> AddBazaarAsync( BazaarRequest bazaarRequest) => bazaarService.AddBazaarAsync(bazaarRequest.ItemInstanceId,
            bazaarRequest.CharacterId, bazaarRequest.CharacterName, bazaarRequest.HasMedal, bazaarRequest.Price, bazaarRequest.IsPackage, bazaarRequest.Duration, bazaarRequest.Amount);

        public Task<BazaarLink?> ModifyBazaarAsync(long id, Json.Patch.JsonPatch bzMod) => bazaarService.ModifyBazaarAsync(id, bzMod);
    }
}