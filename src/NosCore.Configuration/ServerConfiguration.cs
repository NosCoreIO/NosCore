using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Configuration
{
    public class ServerConfiguration : LanguageConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; }

        public override string ToString()
        {
            return Host + ":" + Port;
        }
    }
}
