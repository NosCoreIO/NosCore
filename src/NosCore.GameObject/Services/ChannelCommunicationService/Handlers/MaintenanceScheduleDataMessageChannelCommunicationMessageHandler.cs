//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Services.MaintenanceService;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class MaintenanceScheduleDataMessageChannelCommunicationMessageHandler(IMaintenanceService maintenanceService)
        : ChannelCommunicationMessageHandler<MaintenanceScheduleData>
    {
        public override Task Handle(MaintenanceScheduleData data)
        {
            maintenanceService.ScheduleShutdown(data.ShutdownAt);
            return Task.CompletedTask;
        }
    }
}
