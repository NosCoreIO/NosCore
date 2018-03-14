using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NosCore.Configuration
{
    public class GameServerConfiguration : ServerConfiguration
    {
        public MasterCommunicationConfiguration MasterCommunication { get; set; }
    }
}
