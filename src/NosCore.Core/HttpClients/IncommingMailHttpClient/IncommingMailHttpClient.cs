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
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.WebApi;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClient
{
    public class IncommingMailHttpClient : MasterServerHttpClient, IIncommingMailHttpClient
    {
        private readonly IChannelHttpClient _channelHttpClient;

        public IncommingMailHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/incommingMail";
            RequireConnection = true;
            _channelHttpClient = channelHttpClient;
        }

        public async Task DeleteIncommingMail(int channelId, long id, short mailId, byte postType)
        {
            using var client = await Connect(channelId).ConfigureAwait(false); 
            await client.DeleteAsync(new Uri($"{client.BaseAddress}{ApiUrl}?id={id}&mailId={mailId}&postType={postType}")).ConfigureAwait(false);
        }

        public async Task NotifyIncommingMail(int channelId, MailData mailRequest)
        {
            using var client = await Connect(channelId).ConfigureAwait(false);
            using var content = new StringContent(JsonSerializer.Serialize(mailRequest), Encoding.Default,
                "application/json");
            await client.PostAsync(new Uri($"{client.BaseAddress}{ApiUrl}"), content).ConfigureAwait(false);
        }

        public Task OpenIncommingMail(int channelId, MailData mailData)
        {
            throw new NotImplementedException();
        }
    }
}