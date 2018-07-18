using NosCore.Configuration;

namespace NosCore.Core
{
    public class WorldServerInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        public int ConnectedAccountsLimit { get; set; }

        public ServerConfiguration WebApi { get; set; }
    }
}