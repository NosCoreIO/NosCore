using System;
using System.Collections.Generic;
using NosCore.Configuration;
using NosCore.Data.WebApi;
using NosCore.GameObject.HttpClients.ConnectedAccountHttpClient;

namespace NosCore.Core.HttpClients.ConnectedAccountHttpClient
{
    public class ConnectedAccountHttpClient : IConnectedAccountHttpClient
    {
        public void Disconnect(ServerConfiguration receiverItem1, long connectedCharacterId)
        {
            throw new NotImplementedException();
        }

        public (ServerConfiguration, ConnectedAccount) GetCharacter(long? characterId, string characterName)
        {
            //var channels = MasterClientListSingleton.Instance.Channels ?? Get<List<ChannelInfo>>(WebApiRoute.Channel);
            //foreach (var channel in (channels ?? new List<ChannelInfo>()).Where(c => c.Type == ServerType.WorldServer))
            //{
            //    var accounts = Get<List<ConnectedAccount>>(WebApiRoute.ConnectedAccount, channel.WebApi);

            //    var target = accounts.FirstOrDefault(s => s.ConnectedCharacter.Name == characterName || s.ConnectedCharacter.Id == characterId);

            //    if (target != null)
            //    {
            //        return (channel.WebApi, target);
            //    }
            //}

            return (null, null);
        }

        public List<ConnectedAccount> GetConnectedAccount(ServerConfiguration serverWebApi)
        {
            throw new NotImplementedException();
        }
    }
}
