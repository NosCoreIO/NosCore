//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.ClientPackets.Relations;

namespace NosCore.Data.WebApi
{
    public class BlacklistRequest
    {
        public long CharacterId { get; set; }
        public BlInsPacket? BlInsPacket { get; set; }
    }
}
