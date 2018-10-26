using System.Collections.Generic;
using System.Linq;
using DotNetty.Transport.Channels.Groups;
using NosCore.Core.Serializing;

namespace NosCore.GameObject.Networking.Group
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

            if (matcher == null)
            {
                channelGroup?.WriteAndFlushAsync(PacketFactory.Serialize(packetDefinitions));
            }
            else
            {
                channelGroup?.WriteAndFlushAsync(PacketFactory.Serialize(packetDefinitions), matcher);
            }
        }


        public static void SendPackets(this IChannelGroup channelGroup, IEnumerable<PacketDefinition> packets) =>
            channelGroup.SendPackets(packets, null);
    }
}
