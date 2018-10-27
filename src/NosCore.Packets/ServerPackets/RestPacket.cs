using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("rest")]
    public class RestPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public VisualType VisualType { get; set; }

        [PacketIndex(1)]
        public long VisualId { get; set; }

        [PacketIndex(2)]
        public bool IsSitting { get; set; }
    }
}
