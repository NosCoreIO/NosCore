using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using NosCore.Core.GraphQl;
using NosCore.Data.GraphQl;
using NosCore.GameObject;
using NosCore.GameObject.Networking;

namespace NosCore.WorldServer.GraphQl
{
    public class ConnectedAccountsMutationResolver : IMutationResolver
    {
        public void Resolve(GraphQlMutation graphQlMutation)
        {
            graphQlMutation.Field<ListGraphType<ConnectedAccountType>>(
                "disconnectConnectedAccount",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "name" }
                ),
                resolve: context =>
                {
                    var list = new List<ConnectedAccount>();
                    var name = context.GetArgument<string>("name");
                    var connectedAccount = Broadcaster.Instance.ConnectedAccounts().Find(s => s.ConnectedCharacter.Name == name);
                    if (connectedAccount != null)
                    {
                        list.Add(connectedAccount);
                        (Broadcaster.Instance.GetCharacter(s => s.Name == name) as Character)?.Session.Disconnect();
                    }
                    return list;
                });
        }
    }
}
