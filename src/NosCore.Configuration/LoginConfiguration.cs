using System;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;

namespace NosCore.Configuration
{
    public class LoginConfiguration : GameServerConfiguration
    {
        public SqlConnectionStringBuilder Database { get; set; }
    }
}
