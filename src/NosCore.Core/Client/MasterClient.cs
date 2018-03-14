using NosCore.Configuration;
using NosCore.Master.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core
{
    public class MasterClient
    {
        public ServerType Type { get; set; }
        public string Name { get; set; }
        public ServerConfiguration WebApi { get; set; }
    }
}
