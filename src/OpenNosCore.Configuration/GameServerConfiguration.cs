using System;
using System.Collections.Generic;
using System.Text;

namespace OpenNosCore.Configuration
{
    public class GameServerConfiguration : ServerConfiguration
    {
        public ServerConfiguration WebApi { get; set; }

        public MasterCommunicationConfiguration MasterCommunication { get; set; }
    }
}
