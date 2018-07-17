using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Shared.Enumerations;

namespace NosCore.Data
{
    public class PostedPacket
    {
        public string Packet { get; set; }

        public string SenderCharacterName { get; set; }

        public string ReceiverCharacterName { get; set; }

        public long SenderCharacterId { get; set; }

        public long ReceiverCharacterId { get; set; }

        public int SenderWorldId { get; set; }

        public int ReceiverWorldId { get; set; }

        public MessageType MessageType { get; set; }
    }
}
