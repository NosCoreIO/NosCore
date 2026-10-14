//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class CombatRestHandler(IClock clock)
    {
        [UsedImplicitly]
        public Task Handle(EntityDamagedEvent evt)
        {
            if (evt.Attacker is PlayerComponentBundle attacker)
            {
                attacker.LastAttack = clock.GetCurrentInstant();
            }

            if (evt.Killed || evt.Target is not PlayerComponentBundle target || !target.IsSitting)
            {
                return Task.CompletedTask;
            }

            return target.StandUpAsync();
        }
    }
}
