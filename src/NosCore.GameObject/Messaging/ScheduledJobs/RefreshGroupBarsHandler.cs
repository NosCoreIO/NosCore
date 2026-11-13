//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.ScheduledJobs
{
    [UsedImplicitly]
    public sealed class RefreshGroupBarsHandler(IIdService<Group> groups, ISessionRegistry sessionRegistry)
    {
        [UsedImplicitly]
        public async Task Handle(RefreshGroupBarsMessage _)
        {
            foreach (var group in groups.Items.Values.Where(g => g.Type == GroupType.Group && g.Count > 1))
            {
                var bars = group.GeneratePst();
                foreach (var memberId in group.Keys.Where(k => k.Item1 == VisualType.Player).Select(k => k.Item2).ToList())
                {
                    if (sessionRegistry.TryGetCharacter(s => s.VisualId == memberId, out var member))
                    {
                        await member.SendPacketsAsync(bars.Where(bar => bar.VisualId != memberId));
                    }
                }
            }
        }
    }
}
