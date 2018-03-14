using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace NosCore.Configuration
{
    public class ParserConfiguration
    {
        public SqlConnectionStringBuilder Database { get; set; }
    }
}
