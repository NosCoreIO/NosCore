using NosCore.Configuration;
using System;

namespace NosCore.Core
{
    [Serializable]
    public class Channel
    {
        public string Password { get; set; }

        public string ClientName { get; set; }

        public byte ClientType { get; set; }

        public int Port { get; set; }

        public byte ServerGroup { get; set; }

        public string Host { get; set; }

        public int ConnectedAccountsLimit { get; set; }

        public ServerConfiguration WebApi { get; set; }
    }
}
