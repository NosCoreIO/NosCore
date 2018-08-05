using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("put")]
    public class PutPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public PocketType PocketType { get; set; }

        [PacketIndex(1)]
        public byte Slot { get; set; }

        [PacketIndex(2)]
        public short Amount { get; set; } 
    }
}
