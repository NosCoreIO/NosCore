//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Attributes;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [CommandPacketHeader("$Kick", AuthorityType.Moderator)]
    public class KickPacket : CommandPacket
    {
        [PacketIndex(0)]
        public string? Name { get; set; }

        public override string Help()
        {
            return "$Kick [Name]";
        }
    }
}
