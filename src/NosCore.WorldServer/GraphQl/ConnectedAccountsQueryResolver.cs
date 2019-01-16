using System.Linq;
using GraphQL.Types;
using NosCore.Core.GraphQl;
using NosCore.Data.GraphQl;
using NosCore.GameObject.Networking;

namespace NosCore.WorldServer.GraphQl
{
    public class ConnectedAccountsQueryResolver : IQueryResolver
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
