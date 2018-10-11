using JetBrains.Annotations;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("EntryPoint", 3, AnonymousAccess = true)]
    public class EntryPointPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        [UsedImplicitly]
        public string Title { get; set; }

        [PacketIndex(1)]
        [UsedImplicitly]
        public string Packet1Id { get; set; }

        [PacketIndex(2)]
        public string Name { get; set; }

        [PacketIndex(3)]
        [UsedImplicitly]
        public string Packet2Id { get; set; }

        [PacketIndex(4)]
        public string Password { get; set; }

        #endregion
    }
}