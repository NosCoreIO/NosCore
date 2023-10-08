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

using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Data.WebApi;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NosCore.Core.MessageQueue;
using System.Threading.Channels;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClients
{
    public class ConnectedAccountHttpClient : MasterServerHttpClient, IConnectedAccountHttpClient
    {
        private readonly IPubSubHub _pubSubHub;

        public ConnectedAccountHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient, IPubSubHub pubSubHub)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/connectedAccount";
            RequireConnection = true;
            _pubSubHub = pubSubHub;
        }

        public async Task<Tuple<ServerConfiguration?, Subscriber?>> GetCharacterAsync(long? characterId, string? characterName)
        {
            var servers = await _pubSubHub.GetCommunicationChannels();
            var accounts = await _pubSubHub.GetSubscribersAsync();
            var target = accounts.FirstOrDefault(s => (characterName != null && s.ConnectedCharacter?.Name == characterName) || (characterId != null && s.ConnectedCharacter?.Id == characterId));

            if (target == null)
            {
                return new Tuple<ServerConfiguration?, Subscriber?>(null, null);
            }

            var channel = servers.Where(c => c.Type == ServerType.WorldServer).FirstOrDefault(x => x.Id == target.ChannelId);
            return channel != null ? new Tuple<ServerConfiguration?, Subscriber?>(channel.WebApi, target)
                : new Tuple<ServerConfiguration?, Subscriber?>(null, null);
        }
    }
}