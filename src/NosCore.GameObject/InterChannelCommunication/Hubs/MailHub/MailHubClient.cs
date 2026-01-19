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
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Messages;
using Serilog;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.MailHub
{
    public class MailHubClient(HubConnectionFactory hubConnectionFactory, ILogger logger)
        : BaseHubClient(hubConnectionFactory, nameof(MailHub), logger), IMailHub
    {
        public Task<List<MailData>> GetMails(long id, long characterId, bool senderCopy) =>
            InvokeAsync<List<MailData>>(nameof(GetMails), id, characterId, senderCopy);

        public Task<bool> DeleteMailAsync(long id, long characterId, bool senderCopy) =>
            InvokeAsync<bool>(nameof(DeleteMailAsync), id, characterId, senderCopy);

        public Task<MailData?> ViewMailAsync(long id, JsonPatch mailData) =>
            InvokeAsync<MailData?>(nameof(ViewMailAsync), id, mailData);

        public Task<bool> SendMailAsync(MailRequest mail) =>
            InvokeAsync<bool>(nameof(SendMailAsync), mail);
    }
}
