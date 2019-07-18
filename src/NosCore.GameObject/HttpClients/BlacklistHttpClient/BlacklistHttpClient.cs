using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.BlacklistHttpClient
{
    public class BlacklistHttpClient : MasterServerHttpClient, IBlacklistHttpClient
    {
        public BlacklistHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient) 
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/blacklist";
            RequireConnection = true;
        }

        public List<CharacterRelationStatus> GetBlackLists(long characterVisualId)
        {
            return Get<List<CharacterRelationStatus>>(characterVisualId);
        }

        public LanguageKey AddToBlacklist(BlacklistRequest blacklistRequest)
        {
            return Post<LanguageKey>(blacklistRequest);
        }

        public void DeleteFromBlacklist(Guid characterRelationId)
        {
            Delete(characterRelationId);
        }
    }
}
