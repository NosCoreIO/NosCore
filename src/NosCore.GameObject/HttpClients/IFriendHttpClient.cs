using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients
{
    public interface IFriendHttpClient
    {
        LanguageKey AddFriend(FriendShipRequest friendShipRequest);
    }
}
