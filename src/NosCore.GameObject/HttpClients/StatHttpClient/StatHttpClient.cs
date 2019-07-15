using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.StatHttpClient
{
    public class StatHttpClient : NoscoreHttpClient, IStatHttpClient
    {
        public StatHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {

        }

        public void ChangeStat(StatData data, object item1)
        {
            throw new NotImplementedException();
        }
    }
}
