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

using NosCore.Core;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Data.WebApi;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NosCore.GameObject.HttpClients.PacketHttpClient
{
    public class PacketHttpClient : IPacketHttpClient
    {
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly IHttpClientFactory _httpClientFactory;

        public PacketHttpClient(IHttpClientFactory httpClientFactory, IChannelHttpClient channelHttpClient)
        {
            _channelHttpClient = channelHttpClient;
            _httpClientFactory = httpClientFactory;
        }

        public async Task BroadcastPacketAsync(PostedPacket packet, int channelId)
        {
            var channel = await _channelHttpClient.GetChannelAsync(channelId).ConfigureAwait(false);
            if (channel != null)
            {
                await SendPacketToChannelAsync(packet, channel.WebApi!.ToString()).ConfigureAwait(false);
            }
        }

        public async Task BroadcastPacketAsync(PostedPacket packet)
        {
            var list = (await _channelHttpClient.GetChannelsAsync().ConfigureAwait(false))
                ?.Where(c => c.Type == ServerType.WorldServer) ?? new List<ChannelInfo>();
            await Task.WhenAll(list.Select(channel => SendPacketToChannelAsync(packet, channel.WebApi!.ToString()))).ConfigureAwait(false);

        }

        public Task BroadcastPacketsAsync(List<PostedPacket> packets)
        {
            return Task.WhenAll(packets.Select(packet => BroadcastPacketAsync(packet)));
        }

        public Task BroadcastPacketsAsync(List<PostedPacket> packets, int channelId)
        {
            return Task.WhenAll(packets.Select(packet => BroadcastPacketAsync(packet, channelId)));
        }

        private async Task SendPacketToChannelAsync(PostedPacket postedPacket, string channel)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(channel);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await _channelHttpClient.GetOrRefreshTokenAsync().ConfigureAwait(false));
            var content = new StringContent(JsonSerializer.Serialize(postedPacket),
                Encoding.Default, "application/json");

            await client.PostAsync("api/packet", content).ConfigureAwait(false);
        }
    }
}