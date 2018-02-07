using System;
using System.Collections.Generic;
using System.Text;

namespace OpenNosCore.Configuration
{
    public class BaseConfiguration
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public string WebApi { get; set; }

        public MasterCommunicationConfiguration MasterCommunication { get; set; }
    }
}
