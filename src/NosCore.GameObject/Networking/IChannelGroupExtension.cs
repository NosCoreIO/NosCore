using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetty.Transport.Channels.Groups;
using NosCore.Core.Serializing;

namespace NosCore.GameObject.Networking
{
    public static class IChannelGroupExtension
    {
        public static void SendPacket(this IChannelGroup channelGroup, PacketDefinition packet) 
            => channelGroup.SendPackets(new[] { packet });

        public static void SendPacket(this IChannelGroup channelGroup, PacketDefinition packet, IChannelMatcher matcher)
            => channelGroup.SendPackets(new[] { packet }, matcher);


        public static void SendPackets(this IChannelGroup channelGroup, IEnumerable<PacketDefinition> packets, IChannelMatcher matcher)
        {
            var packetDefinitions = packets as PacketDefinition[] ?? packets.ToArray();
            if (packetDefinitions.Length == 0)
            {
                return;
            }

            channelGroup?.WriteAndFlushAsync(PacketFactory.Serialize(packetDefinitions));
        }


        public static void SendPackets(this IChannelGroup channelGroup, IEnumerable<PacketDefinition> packets) =>
            channelGroup.SendPackets(packets, null);
    }
}
