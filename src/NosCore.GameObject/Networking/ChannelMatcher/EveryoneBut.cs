using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;

namespace NosCore.GameObject.Networking.ChannelMatcher
{
    public class EveryoneBut : IChannelMatcher
    {
        readonly IChannelId _id;

        public EveryoneBut(IChannelId id)
        {
            _id = id;
        }

        public bool Matches(IChannel channel) => channel.Id != _id;
    }
}
