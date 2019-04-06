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
using System.Linq;
using ChickenAPI.Packets.Interfaces;
using DotNetty.Transport.Channels.Groups;

namespace NosCore.GameObject.Networking.Group
{
    public static class IChannelGroupExtension
    {
        public static void SendPacket(this IChannelGroup channelGroup, IPacket packet, ISerializer packetSerializer)
            => channelGroup.SendPackets(new[] {packet}, packetSerializer);

        public static void SendPacket(this IChannelGroup channelGroup, IPacket packet, IChannelMatcher matcher, ISerializer packetSerializer)
            => channelGroup.SendPackets(new[] {packet}, matcher, packetSerializer);


        public static void SendPackets(this IChannelGroup channelGroup, IEnumerable<IPacket> packets,
            IChannelMatcher matcher, ISerializer packetSerializer)
        {
            var packetDefinitions = packets as IPacket[] ?? packets.ToArray();
            if (packetDefinitions.Length == 0)
            {
                return;
            }

            if (matcher == null)
            {
                channelGroup?.WriteAndFlushAsync(packetSerializer.Serialize(packetDefinitions));
            }
            else
            {
                channelGroup?.WriteAndFlushAsync(packetSerializer.Serialize(packetDefinitions), matcher);
            }
        }


        public static void SendPackets(this IChannelGroup channelGroup, IEnumerable<IPacket> packets, ISerializer packetSerializer) =>
            channelGroup.SendPackets(packets, null, packetSerializer);
    }
}