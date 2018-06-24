using NosCore.Core.Serializing;
using System;
using System.Collections.Generic;
using System.Text;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("stat")]
    public class StatPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public int HP { get; set; }

        [PacketIndex(1)]
        public double HPLoad { get; set; }

        [PacketIndex(2)]
        public int MP { get; set; }

        [PacketIndex(3)]
        public double MPLoad { get; set; }

        [PacketIndex(4)]
        public int Unknown { get; set; }

        [PacketIndex(5)]
        public double Option { get; set; }

        #endregion
    }
}
