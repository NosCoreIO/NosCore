//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.FamilyService;

namespace NosCore.GameObject.Messaging.Handlers.Family
{
    [UsedImplicitly]
    public sealed class FamilyRaidWonHandler(IFamilyContributionService familyContributionService)
    {
        [UsedImplicitly]
        public async Task Handle(RaidWonEvent evt)
        {
            foreach (var family in evt.Winners.GroupBy(s => s.FamilyId))
            {
                await familyContributionService.RaidWonAsync(family.Key, family.Select(s => s.CharacterId).ToList(),
                    evt.RaidId, evt.IsHeroic);
            }
        }
    }
}
