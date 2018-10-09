using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Group;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("pjoin")]
    public class PjoinPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public GroupRequestType RequestType { get; set; }

        [PacketIndex(1)]
        [Range(0, long.MaxValue)]
        public long CharacterId { get; set; }
    }
}
