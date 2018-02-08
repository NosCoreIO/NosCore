using System;
using System.Data.SqlClient;

namespace OpenNosCore.Configuration
{
    public class LoginConfiguration : GameServerConfiguration
    {
        public SqlConnectionStringBuilder Database { get; set; }
    }
}
