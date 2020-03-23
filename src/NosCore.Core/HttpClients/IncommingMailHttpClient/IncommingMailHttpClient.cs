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
using Newtonsoft.Json;
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

        public void DeleteIncommingMail(int channelId, long id, short mailId, byte postType)
        {
            using var client = Connect(channelId);
            client.DeleteAsync(new Uri($"{client.BaseAddress}{ApiUrl}?id={id}&mailId={mailId}&postType={postType}")).Wait();
        }

        public void NotifyIncommingMail(int channelId, MailData mailRequest)
        {
            using var client = Connect(channelId);
            using var content = new StringContent(JsonConvert.SerializeObject(mailRequest), Encoding.Default,
                "application/json");
            client.PostAsync(new Uri($"{client.BaseAddress}{ApiUrl}"), content).Wait();
        }

        public void OpenIncommingMail(int channelId, MailData mailData)
        {
            throw new NotImplementedException();
        }
    }
}