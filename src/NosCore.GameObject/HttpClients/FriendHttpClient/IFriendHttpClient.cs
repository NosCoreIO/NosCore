using System;
using System.Collections.Generic;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.FriendHttpClient
{
    public interface IFriendHttpClient
    {
        LanguageKey AddFriend(FriendShipRequest friendShipRequest);
        List<CharacterRelationStatus> GetListFriends(long visualEntityVisualId);
        void DeleteFriend(Guid characterRelationId);
    }
}
