using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using NosCore.Core.GraphQl;
using NosCore.Core.Serializing;
using NosCore.Data.GraphQl;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
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
                        Broadcaster.Instance.GetCharacter(s => s.Name == name).Disconnect();
                    }
                    return list;
                });

            graphQlMutation.Field<ListGraphType<ConnectedAccountType>>(
                "deleteRelationConnectedAccount",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "id" }
                ),
                resolve: context =>
                {
                    var list = new List<ConnectedAccount>();
                    var id = context.GetArgument<string>("id");
                    var connectedAccount = Broadcaster.Instance.GetCharacter(s =>
                        s.CharacterRelations.Any(r => r.Key == Guid.Parse(id)));
                    if (connectedAccount != null)
                    {
                        list.Add(Broadcaster.Instance.ConnectedAccounts().Find(s => s.ConnectedCharacter.Name == connectedAccount.Name));
                        connectedAccount.CharacterRelations.TryRemove(Guid.Parse(id), out var relation);
                        connectedAccount.CharacterRelations.TryRemove(
                            connectedAccount.RelationWithCharacter.Values.First(s => s.RelatedCharacterId == relation.CharacterId)
                                .CharacterRelationId, out _);

                        connectedAccount.SendPacket(connectedAccount.GenerateFinit());
                    }
                    return list;
                });

            graphQlMutation.Field<ListGraphType<ConnectedAccountType>>(
                "SendPacketToConnectedAccount",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "name" },
                              new QueryArgument<StringGraphType> { Name = "id" },
                              new QueryArgument<StringGraphType> { Name = "packet" }
                ),
                resolve: context =>
                {
                    var name = context.GetArgument<string>("name");
                    var id = context.GetArgument<string>("id");
                    var packet = context.GetArgument<string>("packet");
                    List<ConnectedAccount> connectedAccounts = null;
                    if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(name))
                    {
                        connectedAccounts = Broadcaster.Instance.ConnectedAccounts();
                    }
                    else
                    {
                        connectedAccounts = Broadcaster.Instance.ConnectedAccounts().Where(s => s.ConnectedCharacter.Name == name || s.ConnectedCharacter.Id.ToString() == id).ToList();
                    }


                    foreach (var connectedAccount in connectedAccounts)
                    {
                        var message = PacketFactory.Deserialize(packet);
                        var receiverSession = Broadcaster.Instance.GetCharacter(s =>
                            s.Name == connectedAccount.Name);
                        receiverSession.SendPacket(message);
                    }
                    return connectedAccounts;
                });
        }
    }
}
