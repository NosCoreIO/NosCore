﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace NosCore.Configuration
{
    public class MasterConfiguration : WebApiConfiguration
    {
        public SqlConnectionConfiguration Database { get; set; }
    }
}
