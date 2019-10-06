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
using System.Text;
using Newtonsoft.Json;
using NosCore.Core;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;

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

        public void BroadcastPacket(PostedPacket packet, int channelId)
        {
            var channel = _channelHttpClient.GetChannel(channelId);
            if (channel != null)
            {
                SendPacketToChannel(packet, channel.WebApi.ToString());
            }
        }

        public void BroadcastPacket(PostedPacket packet)
        {
            foreach (var channel in _channelHttpClient.GetChannels()
                ?.Where(c => c.Type == ServerType.WorldServer) ?? new List<ChannelInfo>())
            {
                SendPacketToChannel(packet, channel.WebApi.ToString());
            }
        }

        public void BroadcastPackets(List<PostedPacket> packets)
        {
            foreach (var packet in packets)
            {
                BroadcastPacket(packet);
            }
        }

        public void BroadcastPackets(List<PostedPacket> packets, int channelId)
        {
            foreach (var packet in packets)
            {
                BroadcastPacket(packet, channelId);
            }
        }

        private void SendPacketToChannel(PostedPacket postedPacket, string channel)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(channel);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _channelHttpClient.GetOrRefreshToken());
            var content = new StringContent(JsonConvert.SerializeObject(postedPacket),
                Encoding.Default, "application/json");

            client.PostAsync("api/packet", content);
        }
    }
}