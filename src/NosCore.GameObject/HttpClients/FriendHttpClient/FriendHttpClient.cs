using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.FriendHttpClient
{
    public class FriendHttpClient : IFriendHttpClient
    {
        public LanguageKey AddFriend(FriendShipRequest friendShipRequest)
        {
            throw new NotImplementedException();
        }

        public List<CharacterRelationStatus> GetListFriendsStatus(long visualEntityVisualId)
        {
            throw new NotImplementedException();
        }

        public List<CharacterRelation> GetListFriends(long visualEntityVisualId)
        {
            throw new NotImplementedException();
        }

        public void Delete(Guid characterRelationId)
        {
            throw new NotImplementedException();
        }
    }
}
