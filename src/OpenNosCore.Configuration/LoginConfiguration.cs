using System;

namespace OpenNosCore.Configuration
{
    public class LoginConfiguration : BaseConfiguration
    {
        public MasterCommunicationConfiguration MasterCommunication { get; set; }

        public DatabaseConfiguration Database { get; set; }
    }
}
