//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Services.FactionWarService;

namespace NosCore.GameObject.Messaging.ScheduledJobs
{
    [UsedImplicitly]
    public sealed class TickFactionWarHandler(IFactionWarService factionWarService)
    {
        [UsedImplicitly]
        public Task Handle(TickFactionWarMessage _) => factionWarService.TickAsync();
    }
}
