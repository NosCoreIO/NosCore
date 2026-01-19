//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Attributes;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [CommandPacketHeader("$SetMaintenance", AuthorityType.GameMaster)]
    public class SetMaintenancePacket : CommandPacket
    {
        [PacketIndex(0)]
        public bool IsGlobal { get; set; }

        [PacketIndex(1)]
        public bool MaintenanceMode { get; set; }

        public override string Help()
        {
            return "$SetMaintenance IsGlobal MaintenanceMode";
        }
    }
}
