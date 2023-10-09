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
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using NosCore.Core;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.MailService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.MailHub
{
    public class MailHubClient(HubConnectionFactory hubConnectionFactory) : IMailHub
    {
        private readonly HubConnection _hubConnection = hubConnectionFactory.Create(nameof(MailHub));

        public async Task<List<MailData>> GetMails(long id, long characterId, bool senderCopy)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<List<MailData>>(nameof(GetMails), id, characterId, senderCopy);
            await _hubConnection.StopAsync();
            return result;
        }
        public async Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<bool>(nameof(DeleteMailAsync), id, characterId, senderCopy);
            await _hubConnection.StopAsync();
            return result;
        }
        public async Task<MailData?> ViewMailAsync(long id, JsonPatch mailData)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<MailData?>(nameof(ViewMailAsync), id, mailData);
            await _hubConnection.StopAsync();
            return result;
        }
        public async Task<bool> SendMailAsync(MailRequest mail)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<bool>(nameof(SendMailAsync), mail);
            await _hubConnection.StopAsync();
            return result;
        }
    }
}