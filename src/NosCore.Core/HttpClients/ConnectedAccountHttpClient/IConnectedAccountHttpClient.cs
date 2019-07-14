using System.Collections.Generic;
using NosCore.Configuration;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.ConnectedAccountHttpClient
{
    public interface IConnectedAccountHttpClient
    {
        List<ConnectedAccount> GetConnectedAccount(ServerConfiguration serverWebApi);
        void Disconnect(ServerConfiguration receiverItem1, long connectedCharacterId);

        (ServerConfiguration, ConnectedAccount) GetCharacter(long? characterId, string characterName);  
    }
}
