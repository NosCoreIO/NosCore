//  __  _  __    __   ___ __  ___ ___	
// |  \| |/__\ /' _/ / _//__\| _ \ __|	
// | | ' | \/ |`._`.| \_| \/ | v / _|	
// |_|\__|\__/ |___/ \__/\__/|_|_\___|	
// -----------------------------------	

using NosCore.Packets;
using NosCore.Packets.Attributes;
using NosCore.Packets.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [PacketHeader("$fl", Scope.InTrade | Scope.InGame)]
    public class FlPacket : CommandPacket
    {
        [PacketIndex(0)]
        public string? CharacterName { get; set; }

        public override string Help()
        {
            return "$fl [CharacterName]";
        }
    }
}