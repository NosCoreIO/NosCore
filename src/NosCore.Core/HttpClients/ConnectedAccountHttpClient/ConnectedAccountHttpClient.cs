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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using NosCore.Configuration;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClient
{
    public class ConnectedAccountHttpClient : MasterServerHttpClient, IConnectedAccountHttpClient
    {
        private readonly IChannelHttpClient _channelHttpClient;

        public ConnectedAccountHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            _channelHttpClient = channelHttpClient;
            ApiUrl = "api/connectedAccount";
            RequireConnection = true;
        }

        public void Disconnect(long connectedCharacterId)
        {
            Delete(connectedCharacterId);
        }

        public (ServerConfiguration, ConnectedAccount) GetCharacter(long? characterId, string characterName)
        {
            foreach (var channel in _channelHttpClient.GetChannels().Where(c => c.Type == ServerType.WorldServer))
            {
                var accounts = GetConnectedAccount(channel);
                var target = accounts.FirstOrDefault(s =>
                    (s.ConnectedCharacter.Name == characterName) || (s.ConnectedCharacter.Id == characterId));

                if (target != null)
                {
                    return (channel.WebApi, target);
                }
            }

            return (null, null);
        }

        public List<ConnectedAccount> GetConnectedAccount(ChannelInfo channel)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(channel.WebApi.ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channel.Token);

            var response = client.GetAsync("api/connectedAccount").Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<List<ConnectedAccount>>(
                    response.Content.ReadAsStringAsync().Result);
            }

            return new List<ConnectedAccount>();
        }
    }
}