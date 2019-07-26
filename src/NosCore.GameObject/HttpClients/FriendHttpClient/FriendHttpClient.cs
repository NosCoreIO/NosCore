using System;
using System.Collections.Generic;
using System.Net.Http;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.FriendHttpClient
{
    public class FriendHttpClient : MasterServerHttpClient, IFriendHttpClient
    {
        public FriendHttpClient(IHttpClientFactory httpClientFactory, Channel channel, IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/friend";
            RequireConnection = true;
        }

        public LanguageKey AddFriend(FriendShipRequest friendShipRequest)
        {
            return Post<LanguageKey>(friendShipRequest);
        }

        public List<CharacterRelationStatus> GetListFriends(long visualEntityVisualId)
        {
            return Get<List<CharacterRelationStatus>>(visualEntityVisualId);
        }

        public void DeleteFriend(Guid characterRelationId)
        {
            Delete(characterRelationId);
        }
    }
}
