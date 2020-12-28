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

using Json.Patch;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.HttpClients.BazaarHttpClient
{
    public interface IBazaarHttpClient
    {
        Task<List<BazaarLink>> GetBazaarLinksAsync(int i, int packetIndex, int pagesize, BazaarListType packetTypeFilter,
            byte packetSubTypeFilter, byte packetLevelFilter,
            byte packetRareFilter, byte packetUpgradeFilter, long? sellerFilter);

        Task<LanguageKey?> AddBazaarAsync(BazaarRequest bazaarRequest);
        Task<BazaarLink?> GetBazaarLinkAsync(long bazaarId);
        Task<bool> RemoveAsync(long bazaarId, int count, string requestCharacterName);
        Task<BazaarLink> ModifyAsync(long bazaarId, JsonPatch patchBz);
    }
}