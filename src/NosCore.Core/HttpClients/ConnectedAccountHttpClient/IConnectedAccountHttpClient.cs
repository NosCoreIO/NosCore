using NosCore.Configuration;
using NosCore.Data.WebApi;
using System.Collections.Generic;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClient
{
    public interface IConnectedAccountHttpClient
    {
        List<ConnectedAccount> GetConnectedAccount(ChannelInfo channel);
        void Disconnect(long connectedCharacterId);

        (ServerConfiguration, ConnectedAccount) GetCharacter(long? characterId, string characterName);
    }
}
