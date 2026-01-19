//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [CommandPacketHeader("$Invisible", AuthorityType.GameMaster)]
    public class InvisibleCommandPacket : CommandPacket
    {
        public override string Help()
        {
            return "$Invisible";
        }
    }
}
