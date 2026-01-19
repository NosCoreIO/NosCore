//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets;
using NosCore.Packets.Attributes;
using NosCore.Packets.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [PacketHeader("EntryPoint", Scope.OnCharacterScreen)]
    public class EntryPointPacket : PacketBase
    {
        [PacketIndex(1)]
        public required string Name { get; set; }

        [PacketIndex(2)]
        public string? Password { get; set; }
    }

}
