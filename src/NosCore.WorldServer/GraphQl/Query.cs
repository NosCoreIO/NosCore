using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;

namespace NosCore.WorldServer.GraphQl
{
    //[AuthorizeRole(AuthorityType.GameMaster)]
    public class Query
    {
        public IEnumerable<ConnectedAccount> GetConnectedAccounts(string name)
        {
            return Broadcaster.Instance.ConnectedAccounts();
        }
    }
}
