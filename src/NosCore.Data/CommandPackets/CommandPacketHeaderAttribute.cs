//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Attributes;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.CommandPackets
{
    public class CommandPacketHeaderAttribute(string identification, AuthorityType authority) : PacketHeaderAttribute(identification, Scope.InGame)
    {
        public AuthorityType Authority { get; } = authority;
    }
}
