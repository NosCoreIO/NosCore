using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Types;
using NosCore.Core.GraphQl;
using NosCore.Data.GraphQl;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations.Group;

namespace NosCore.WorldServer.GraphQl
{
    public class ConnectedAccountsResolver : IQueryResolver
    {
        public void Resolve(GraphQlQuery graphQlQuery)
        {
            graphQlQuery.Field<ListGraphType<ConnectedAccountType>>(
                "connectedAccounts",
                resolve: context => Broadcaster.Instance.ConnectedAccounts()
            );
        }
    }
}
