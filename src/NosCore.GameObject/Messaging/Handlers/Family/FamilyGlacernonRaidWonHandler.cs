//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.FamilyService;

namespace NosCore.GameObject.Messaging.Handlers.Family
{
    [UsedImplicitly]
    public sealed class FamilyGlacernonRaidWonHandler(IFamilyContributionService familyContributionService)
    {
        [UsedImplicitly]
        public Task Handle(GlacernonRaidWonEvent evt) =>
            familyContributionService.GlacernonRaidWonAsync(evt.FamilyId, evt.Raid, evt.InTenMinutes, evt.WithoutLoss);
    }
}
