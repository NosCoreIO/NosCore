using NosCore.Core.Serializing;
using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Configuration;

namespace NosCore.WebApiData
{
    public class PostedPacket
    {
        public string Packet { get; set; }

        public string Sender { get; set; }

        public string Receiver { get; set; }

        public ServerConfiguration WebApi { get; set; }
    }
}
