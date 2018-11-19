using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("exc_list")]
    public class ExcListPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public int Unknown { get; set; }

        [PacketIndex(1)]
        public long VisualId { get; set; }

        [PacketIndex(2)]
        public long Gold { get; set; }

        [PacketIndex(3, IsOptional = true)]
        public long BankGold { get; set; }

        [PacketIndex(4, IsOptional = true)]
        public ExcListSubPacket SubPackets { get; set; }
    }
}
