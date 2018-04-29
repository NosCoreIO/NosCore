using NosCore.Core.Serializing;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("scene")]
    public class ScenePacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte SceneId { get; set; }      
    }
}
