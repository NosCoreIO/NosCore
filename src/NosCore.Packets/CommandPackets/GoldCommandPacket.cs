using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$Gold", Authority = AuthorityType.GameMaster)]
    public class GoldCommandPacket : PacketDefinition, ICommandPacket
    {
        [PacketIndex(0)]
        public long Gold { get; set; }

        public string Help()
        {
            return "$Gold value";
        }
    }
}