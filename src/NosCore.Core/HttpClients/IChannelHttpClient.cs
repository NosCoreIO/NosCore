using System.Net;
using NosCore.Configuration;
using NosCore.Data.WebApi;

namespace NosCore.Core.HttpClients
{
    public interface IChannelHttpClient
    {
        void Connect();
        HttpStatusCode Ping();
        string GetOrRefreshToken();
        (ServerConfiguration, ConnectedAccount) GetCharacter(long? characterId, string characterName);
    }
}