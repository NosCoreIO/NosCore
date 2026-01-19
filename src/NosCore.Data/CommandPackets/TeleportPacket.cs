//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Attributes;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [CommandPacketHeader("$Teleport", AuthorityType.GameMaster)]
    public class TeleportPacket : CommandPacket
    {
        [PacketIndex(0)]
        public string? TeleportArgument { get; set; }

        [PacketIndex(1)]
        public short? MapX { get; set; }

        [PacketIndex(2)]
        public short? MapY { get; set; }

        public override string Help()
        {
            return "$Teleport TeleportArgument [X] [Y]";
        }
    }
}
