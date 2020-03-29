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
using System.Threading.Tasks;
using NosCore.Packets.Interfaces;
using DotNetty.Transport.Channels.Groups;

namespace NosCore.GameObject.Networking.Group
{
    public static class IBroadcastableExtension
    {
        public static Task SendPacketAsync(this IBroadcastable channelGroup, IPacket packet)
        {
            return channelGroup.SendPacketsAsync(new[] { packet });
        }

        public static Task SendPacketAsync(this IBroadcastable channelGroup, IPacket packet, IChannelMatcher matcher)
        {
           return channelGroup.SendPacketsAsync(new[] { packet }, matcher);
        }


        public static async Task SendPacketsAsync(this IBroadcastable channelGroup, IEnumerable<IPacket> packets,
            IChannelMatcher? matcher)
        {
            var packetDefinitions = (packets as IPacket[] ?? packets).Where(c => c != null).ToArray();
            if (packetDefinitions.Any())
            {
                Parallel.ForEach(packets, packet => channelGroup.LastPackets.Enqueue(packet));
                Parallel.For(0, channelGroup.LastPackets.Count - channelGroup.MaxPacketsBuffer, (_, __) => channelGroup.LastPackets.TryDequeue(out var ___));
                if (channelGroup?.Sessions == null)
                {
                    return;
                }

                await channelGroup.Sessions.WriteAndFlushAsync(packetDefinitions).ConfigureAwait(false);
                if (matcher == null)
                {
                    await channelGroup.Sessions.WriteAndFlushAsync(packetDefinitions).ConfigureAwait(false);
                }
                else
                {
                    await channelGroup.Sessions.WriteAndFlushAsync(packetDefinitions, matcher).ConfigureAwait(false);
                }
            }
        }


        public static Task SendPacketsAsync(this IBroadcastable channelGroup, IEnumerable<IPacket> packets)
        {
            return channelGroup.SendPacketsAsync(packets, null);
        }
    }
}