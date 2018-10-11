using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$Speed", Authority = AuthorityType.GameMaster)]
    public class SpeedPacket : PacketDefinition, ICommandPacket
    {
        [PacketIndex(0)]
        public byte Speed { get; set; }

        public string Help()
        {
            return "$Speed value";
        }
    }
}