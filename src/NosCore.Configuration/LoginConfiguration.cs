using System;
using System.Data.SqlClient;

namespace NosCore.Configuration
{
    public class LoginConfiguration : GameServerConfiguration
    {
        public SqlConnectionConfiguration Database { get; set; }
    }
}
