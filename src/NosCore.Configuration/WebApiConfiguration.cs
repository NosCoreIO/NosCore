using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NosCore.Configuration
{
    public class WebApiConfiguration : GameServerConfiguration
    {
        public string Password { get; set; }
        public ServerConfiguration WebApi { get; set; }
    }
}
