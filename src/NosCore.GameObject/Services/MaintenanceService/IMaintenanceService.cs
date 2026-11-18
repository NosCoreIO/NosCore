//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MaintenanceService
{
    public interface IMaintenanceService
    {
        // With a delay, the players are warned and maintenance starts once it is over.
        Task SetMaintenanceAsync(bool isGlobal, bool value, Duration? delay);

        // The time this channel shuts down at; null cancels a pending shutdown.
        void ScheduleShutdown(Instant? shutdownAt);

        Task TickAsync();
    }
}
