using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace NosCore.Configuration
{
    public class WorldConfiguration : WebApiConfiguration
    {
        public SqlConnectionStringBuilder Database { get; set; }

        public short ConnectedAccountLimit { get; set; }

        public byte ServerGroup { get; set; }

        public bool WorldInformation { get; set; }
        public bool SceneOnCreate { get; set; }
    }
}
