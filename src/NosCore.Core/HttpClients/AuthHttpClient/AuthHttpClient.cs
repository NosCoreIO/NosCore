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
using Newtonsoft.Json;
using NosCore.Core.HttpClients.ChannelHttpClient;

namespace NosCore.Core.HttpClients.AuthHttpClient
{
    public class AuthHttpClient : MasterServerHttpClient, IAuthHttpClient
    {
        public AuthHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/auth";
            RequireConnection = true;
        }

        public string? GetAwaitingConnection(string name, string packetPassword, int clientSessionSessionId)
        {
            var client = Connect();
            var response = client
                .GetAsync(new Uri($"{client.BaseAddress}{ApiUrl}?id={name}&token={packetPassword}&sessionId={clientSessionSessionId}")).Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<string?>(response.Content.ReadAsStringAsync().Result);
            }

            throw new HttpRequestException(response.Headers.ToString());
        }
    }
}