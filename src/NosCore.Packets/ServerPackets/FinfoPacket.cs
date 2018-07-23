using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("finfo")]
    public class FinfoPacket : PacketDefinition
    {
        [PacketIndex(0, SpecialSeparator = ".")]
        public long RelatedCharacterId { get; set; }

        [PacketIndex(1, SpecialSeparator = ".")]
        public bool IsConnected { get; set; }
    }
}
