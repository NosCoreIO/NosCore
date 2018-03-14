using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("EntryPoint", 3)]
    public class EntryPointPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public string Title { get; set; }

        [PacketIndex(1)]
        public string Packet1Id { get; set; }

        [PacketIndex(2)]
        public string Name { get; set; }

        [PacketIndex(3)]
        public string Packet2Id { get; set; }

        [PacketIndex(4)]
        public string Password { get; set; }
        #endregion

    }
}