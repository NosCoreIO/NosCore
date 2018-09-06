using System;
using System.Collections.Generic;
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
        public long CharacterId { get; set; }
    }
}
