using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Account;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$Gold", Authority = AuthorityType.GameMaster)]
    public class GoldCommandPacket : PacketDefinition, ICommandPacket
    {
        [PacketIndex(0)]
        [Range(1, 1000000000)]
        public long Gold { get; set; }

        public string Help()
        {
            return "$Gold value";
        }
    }
}