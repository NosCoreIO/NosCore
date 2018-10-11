using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations;

namespace NosCore.Packets.ServerPackets
{
    [PacketHeader("get")]
    public class ServerGetPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public VisualType VisualType { get; set; }

        [PacketIndex(1)]
        public long VisualId { get; set; }

        [PacketIndex(2)]
        public long ItemId { get; set; }

        [PacketIndex(3)]
        public long Unknow { get; set; }
    }
}
