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
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BazaarService
{
    public interface IBazaarService
    {
        void Initialize(IDao<BazaarItemDto, long> bazaarItemDao, IDao<IItemInstanceDto?, Guid> itemInstanceDao, IDao<CharacterDto, long> characterDao);

        List<BazaarLink> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter);

        Task<bool> DeleteBazaarAsync(long id, short count, string requestCharacterName);

        Task<LanguageKey> AddBazaarAsync(Guid itemInstanceId, long characterId, string? characterName, bool hasMedal, long price, bool isPackage, short duration, short amount);

        Task<BazaarLink?> ModifyBazaarAsync(long id, Json.Patch.JsonPatch bzMod);
    }
}