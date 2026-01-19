//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Attributes;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [CommandPacketHeader("$ChangeClass", AuthorityType.GameMaster)]
    public class ChangeClassPacket : CommandPacket
    {
        [PacketIndex(0)]
        public CharacterClassType ClassType { get; set; }

        [PacketIndex(1)]
        public string? Name { get; set; }

        public override string Help()
        {
            return "$ChangeClass ClassType [Name]";
        }
    }
}
