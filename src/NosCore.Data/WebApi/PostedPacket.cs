using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.WebApi
{
    public class PostedPacket
    {
        public string Packet { get; set; }

        public CharacterData SenderCharacterData { get; set; }

        public CharacterData ReceiverCharacterData { get; set; }

        public int OriginWorldId { get; set; }

        public MessageType MessageType { get; set; }
    }
}
