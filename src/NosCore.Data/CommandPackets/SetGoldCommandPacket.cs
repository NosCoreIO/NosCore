//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Attributes;
using NosCore.Shared.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace NosCore.Data.CommandPackets
{
    [CommandPacketHeader("$SetGold", AuthorityType.GameMaster)]
    [PacketHeaderAlias("$Gold")]
    public class SetGoldCommandPacket : CommandPacket
    {
        [PacketIndex(0)]
        [Range(1, 1000000000)]
        public long Gold { get; set; }

        [PacketIndex(1)]
        public string? Name { get; set; }

        public override string Help()
        {
            return "$Gold Gold [Name]";
        }
    }
}
