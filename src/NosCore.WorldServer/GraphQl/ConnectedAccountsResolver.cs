using System;
using System.Collections.Generic;
using System.Linq;
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
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "name" }
                    ),
                resolve: context =>
                {
                    var name = context.GetArgument<string>("name");
                    return Broadcaster.Instance.ConnectedAccounts().Where(s => name == null || s.ConnectedCharacter.Name == name);
                });
        }
    }
}
