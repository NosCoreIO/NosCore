//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Attributes;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [CommandPacketHeader("$Speed", AuthorityType.GameMaster)]
    public class SpeedPacket : CommandPacket
    {
        [PacketIndex(0)]
        public byte Speed { get; set; }

        public override string Help()
        {
            return "$Speed Speed";
        }
    }
}
