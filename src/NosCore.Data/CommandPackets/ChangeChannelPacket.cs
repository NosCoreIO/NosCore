//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Attributes;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [CommandPacketHeader("$ChangeChannel", AuthorityType.GameMaster)]
    public class ChangeChannelPacket : CommandPacket
    {
        [PacketIndex(0)]
        public int ChannelId { get; set; }

        public override string Help()
        {
            return "$ChangeChannel ChannelId";
        }
    }
}
