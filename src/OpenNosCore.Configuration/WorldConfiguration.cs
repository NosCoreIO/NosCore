using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Text;

namespace OpenNosCore.Configuration
{
    public class WorldConfiguration : WebApiConfiguration
    {
        public SqlConnectionStringBuilder Database { get; set; }

        public short ConnectedAccountLimit { get; set; }

        public byte ServerGroup { get; set; }
    }
}
