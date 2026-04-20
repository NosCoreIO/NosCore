//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    // When a monster takes damage, bump its aggro toward the attacker. Players getting
    // hit is ignored here — player UI/combat feedback is handled by the SuPacket
    // broadcast in BattleService. Wolverine auto-discovers this handler by convention.
    [UsedImplicitly]
    public sealed class MonsterAggroHandler(IAggroService aggroService)
    {
        [UsedImplicitly]
        public Task Handle(EntityDamagedEvent evt)
        {
            if (evt.Target is not INonPlayableEntity)
            {
                return Task.CompletedTask;
            }

            aggroService.AddThreat(evt.Target, evt.Attacker, evt.Damage);
            return Task.CompletedTask;
        }
    }
}
