using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Data
{
    public class PostedPacket
    {
        public string Packet { get; set; }

        public string Sender { get; set; }

        public string Receiver { get; set; }

        public int SenderWorldId { get; set; }

        public int ReceiverWorldId { get; set; }
    }
}
