using NosCore.Core.Serializing;
using NosCore.Shared.Account;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$Speed", Authority = AuthorityType.GameMaster)]
    public class SpeedPacket : PacketDefinition
    {
        [PacketIndex(0)]
        public byte Speed { get; set; }
    }
}