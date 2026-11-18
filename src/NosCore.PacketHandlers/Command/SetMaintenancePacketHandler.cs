//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MaintenanceService;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class SetMaintenancePacketHandler
        (IMaintenanceService maintenanceService) : PacketHandler<SetMaintenancePacket>,
            IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SetMaintenancePacket setMaintenancePacket, ClientSession session)
        {
            await maintenanceService.SetMaintenanceAsync(setMaintenancePacket.IsGlobal, setMaintenancePacket.MaintenanceMode,
                setMaintenancePacket.DelayInSeconds is { } seconds ? Duration.FromSeconds(seconds) : null);
        }
    }
}
