using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpenNosCore.Configuration
{
    public class GameServerConfiguration : ServerConfiguration
    {
        public MasterCommunicationConfiguration MasterCommunication { get; set; }
    }
}
