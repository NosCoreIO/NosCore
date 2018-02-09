using System;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;

namespace OpenNosCore.Configuration
{
    public class LoginConfiguration : GameServerConfiguration
    {
        public SqlConnectionStringBuilder Database { get; set; }
    }
}
