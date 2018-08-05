using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("get")]
    public class GetPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public PickerType PickerType { get; set; }

        [PacketIndex(1)]
        public int PickerId { get; set; }

        [PacketIndex(2)]
        public long VisualId { get; set; }
    }
}
