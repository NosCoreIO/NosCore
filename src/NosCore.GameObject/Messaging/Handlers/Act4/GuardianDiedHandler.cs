//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.FactionWarService;

namespace NosCore.GameObject.Messaging.Handlers.Act4
{
    [UsedImplicitly]
    public sealed class GuardianDiedHandler(IFactionWarService factionWarService)
    {
        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt) =>
            evt.Victim is MonsterComponentBundle { VNum: FactionWarService.Mukraju, MapInstance.IsAct4: true } monster
                ? factionWarService.GuardianDiedAsync(monster)
                : Task.CompletedTask;
    }
}
