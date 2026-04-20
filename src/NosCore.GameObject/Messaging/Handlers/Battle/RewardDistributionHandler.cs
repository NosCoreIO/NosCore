//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    // Single responsibility: when an entity dies, hand the HitList to the reward service
    // so it can credit XP/gold/drops. Split out of the damage hot path as a Wolverine
    // handler so slow drops / XP calculations never block an in-flight hit.
    [UsedImplicitly]
    public sealed class RewardDistributionHandler(IRewardService rewardService)
    {
        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt)
        {
            return rewardService.DistributeAsync(evt.Victim, evt.Killer);
        }
    }
}
