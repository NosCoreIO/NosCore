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
    [CommandPacketHeader("$SetJobLevel", AuthorityType.GameMaster)]
    [PacketHeaderAlias("$JLvl")]
    public class SetJobLevelCommandPacket : CommandPacket
    {
        [PacketIndex(0)]
        [Range(1, byte.MaxValue)]
        public byte Level { get; set; }

        [PacketIndex(1)]
        public string? Name { get; set; }

        public override string Help()
        {
            return "$SetJobLevel Level [Name]";
        }
    }
}
