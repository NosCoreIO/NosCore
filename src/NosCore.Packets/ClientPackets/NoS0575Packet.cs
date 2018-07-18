using JetBrains.Annotations;
using NosCore.Core.Serializing;

namespace NosCore.Packets.ClientPackets
{
    [PacketHeader("NoS0575", AnonymousAccess = true)]
    public class NoS0575Packet : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        [UsedImplicitly]
        public int Number { get; set; }

        [PacketIndex(1)]
        public string Name { get; set; }

        [PacketIndex(2)]
        public string Password { get; set; }

        [PacketIndex(3)]
        public string ClientData { get; set; }

        #endregion
    }
}