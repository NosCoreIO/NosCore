using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Core.Networking
{
    public class NetworkPacket
    {
        public string Message { get; set; }

        public Encoding Encoding { get; set; } = Encoding.Default;
    }
}
