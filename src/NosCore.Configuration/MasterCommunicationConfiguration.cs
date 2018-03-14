using System;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Configuration
{
    public class MasterCommunicationConfiguration : ServerConfiguration
    {
        public string Password { get; set; }
        public ServerConfiguration WebApi { get; set; }
    }
}
