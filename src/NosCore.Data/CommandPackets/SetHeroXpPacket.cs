//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Attributes;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [CommandPacketHeader("$SetHeroXp", AuthorityType.GameMaster)]
    public class SetHeroXpPacket : CommandPacket
    {
        [PacketIndex(0)]
        public long HeroXp { get; set; }

        public override string Help()
        {
            return "$SetHeroXp Amount";
        }
    }
}
