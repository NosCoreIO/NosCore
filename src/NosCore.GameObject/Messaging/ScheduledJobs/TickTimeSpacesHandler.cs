//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Services.ScriptedInstanceService;

namespace NosCore.GameObject.Messaging.ScheduledJobs
{
    [UsedImplicitly]
    public sealed class TickTimeSpacesHandler(IScriptedInstanceService scriptedInstanceService)
    {
        [UsedImplicitly]
        public Task Handle(TickTimeSpacesMessage _) => scriptedInstanceService.TickAsync();
    }
}
