using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("ncif")]
    public class NcifPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public VisualType Type { get; set; }

        [PacketIndex(1)]
        [Range(0, long.MaxValue)]
        public long TargetId { get; set; }
    }
}
