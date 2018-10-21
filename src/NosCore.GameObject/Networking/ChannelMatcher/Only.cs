using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;

namespace NosCore.GameObject.Networking.ChannelMatcher
{
    public class Only : IChannelMatcher
    {
        readonly IChannelId _id;

        public Only(IChannelId id)
        {
            _id = id;
        }

        public bool Matches(IChannel channel) => channel.Id == _id;
    }
}
