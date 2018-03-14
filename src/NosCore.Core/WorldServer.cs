using NosCore.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core
{
    public class WorldServer 
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        public int ConnectedAccountsLimit { get; set; }

        public ServerConfiguration WebApi { get; set; }

        public WorldServer()
        {

        }
    }
}
