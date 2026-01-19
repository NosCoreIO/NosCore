//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Attributes;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [CommandPacketHeader("$CreateItem", AuthorityType.GameMaster)]
    public class CreateItemPacket : CommandPacket
    {
        [PacketIndex(0)]
        public short VNum { get; set; }

        [PacketIndex(1)]
        public short? DesignOrAmount { get; set; }

        [PacketIndex(2)]
        public byte? Upgrade { get; set; }

        public override string Help()
        {
            return "$CreateItem VNum [DesignOrAmount] [Upgrade]";
        }
    }
}
