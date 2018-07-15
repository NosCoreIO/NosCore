using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Map;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("spk")]
    public class SpeakPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public VisualType VisualType { get; set; }

        [PacketIndex(1)]
        public long VisualId { get; set; }

        [PacketIndex(2)]
        public SpeakType SpeakType { get; set; }
        
        [PacketIndex(3)]
        public string EntityName { get; set; }

        [PacketIndex(4, SerializeToEnd = true)]
        public string Message { get; set; }
    }
}
