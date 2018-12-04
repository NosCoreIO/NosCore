using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("exc_list")] //TODO: finish this packet
    public class ExcListPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public int Unknown { get; set; } //TODO: find what this is for

        [PacketIndex(1)]
        [Range(0, long.MaxValue)]
        public long VisualId { get; set; }

        [PacketIndex(2)]
        [Range(-1, long.MaxValue)]
        public long Gold { get; set; }

        [PacketIndex(3, IsOptional = true)]
        [Range(0, long.MaxValue)]
        public long BankGold { get; set; }

        [PacketIndex(4, IsOptional = true)]
        public List<ExcListSubPacket> SubPackets { get; set; }
    }
}
