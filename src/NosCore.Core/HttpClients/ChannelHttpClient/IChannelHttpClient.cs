using System.Collections.Generic;
using System.Net;

namespace NosCore.Core.HttpClients.ChannelHttpClient
{
    public interface IChannelHttpClient
    {
        void Connect();
        HttpStatusCode Ping();
        string GetOrRefreshToken();
        List<ChannelInfo> GetChannels();
        ChannelInfo GetChannel(int channelId);
    }
}