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
using Microsoft.AspNetCore.SignalR.Client;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.BazaarHub
{
    public class BazaarHubClient(HubConnectionFactory hubConnectionFactory) : IBazaarHub
    {
        private readonly HubConnection _hubConnection = hubConnectionFactory.Create(nameof(BazaarHub));

        public async Task<List<BazaarLink>> GetBazaar(long id, byte? index, byte? pageSize, BazaarListType? typeFilter,
            byte? subTypeFilter, byte? levelFilter, byte? rareFilter, byte? upgradeFilter, long? sellerFilter)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<List<BazaarLink>>(nameof(GetBazaar), id, index, pageSize, typeFilter,
                subTypeFilter, levelFilter, rareFilter, upgradeFilter, sellerFilter);
            await _hubConnection.StopAsync();
            return result;
        }

        public async Task<bool> DeleteBazaarAsync(long id, short count, string requestCharacterName)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<bool>(nameof(DeleteBazaarAsync), id, count, requestCharacterName);
            await _hubConnection.StopAsync();
            return result;
        }

        public async Task<LanguageKey> AddBazaarAsync(BazaarRequest bazaarRequest)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<LanguageKey>(nameof(AddBazaarAsync), bazaarRequest);
            await _hubConnection.StopAsync();
            return result;
        }

        public async Task<BazaarLink?> ModifyBazaarAsync(long id, Json.Patch.JsonPatch bzMod)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<BazaarLink?>(nameof(ModifyBazaarAsync), id, bzMod);
            await _hubConnection.StopAsync();
            return result;
        }
    }
}