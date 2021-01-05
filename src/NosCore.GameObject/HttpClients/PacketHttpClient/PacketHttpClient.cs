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

using NosCore.Data.WebApi;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace NosCore.GameObject.HttpClients.PacketHttpClient
{
    public class PacketHttpClient : IPacketHttpClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PacketHttpClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public Task BroadcastPacketAsync(PostedPacket packet, int channelId)
        {
            return Task.CompletedTask;
        }

        public Task BroadcastPacketAsync(PostedPacket packet)
        {
            return Task.CompletedTask;

        }

        public Task BroadcastPacketsAsync(List<PostedPacket> packets)
        {
            return Task.CompletedTask;
        }

        public Task BroadcastPacketsAsync(List<PostedPacket> packets, int channelId)
        {
            return Task.CompletedTask;
        }

        private Task SendPacketToChannelAsync(PostedPacket postedPacket, string channel)
        {
            return Task.CompletedTask;
        }
    }
}