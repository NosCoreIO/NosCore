//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BaseCampService;

namespace NosCore.GameObject.Messaging.Handlers.Act4
{
    [UsedImplicitly]
    public sealed class BaseCampFellHandler(IBaseCampService baseCampService)
    {
        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is MonsterComponentBundle { MapInstance.IsAct4: true } monster)
            {
                baseCampService.CampFell(monster);
            }

            return Task.CompletedTask;
        }
    }
}
