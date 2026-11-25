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
    [CommandPacketHeader("$Ban", AuthorityType.Moderator)]
    public class BanPacket : CommandPacket
    {
        [PacketIndex(0)]
        public string? Name { get; set; }

        [PacketIndex(1)]
        [Range(0, 36500)]
        public int Days { get; set; }

        [PacketIndex(2, IsOptional = true)]
        [StringLength(255)]
        public string? Reason { get; set; }

        public override string Help()
        {
            return "$Ban Name Days(0 = for good) [Reason]";
        }
    }
}
