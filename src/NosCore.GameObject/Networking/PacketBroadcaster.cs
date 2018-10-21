//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Linq;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.Networking
{
    public class PacketBroadcaster //TODO move to a service
    {
        private static PacketBroadcaster _instance;
        private PacketBroadcaster()
        {
        }

        public static PacketBroadcaster Instance => _instance ?? (_instance = new PacketBroadcaster());

        public void BroadcastPacket(PostedPacket postedPacket)
        {
            foreach (var channel in WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels"))
            {
                WebApiAccess.Instance.Post<PostedPacket>("api/packet", postedPacket, channel.WebApi);
            }
        }
        public void BroadcastPacket(PostedPacket postedPacket, int channelId)
        {
            var channel = WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels", channelId).FirstOrDefault();
            if (channel != null)
            {
                WebApiAccess.Instance.Post<PostedPacket>("api/packet", postedPacket, channel.WebApi);
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
    }
}