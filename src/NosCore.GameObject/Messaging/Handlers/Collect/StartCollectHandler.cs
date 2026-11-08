//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.CollectService;

namespace NosCore.GameObject.Messaging.Handlers.Collect
{
    [UsedImplicitly]
    public sealed class StartCollectHandler(ICollectService collectService)
    {
        [UsedImplicitly]
        public Task Handle(NpcTalkedEvent evt)
        {
            return evt.Character is PlayerComponentBundle character && evt.Npc is NpcComponentBundle npc
                ? collectService.StartAsync(character, npc)
                : Task.CompletedTask;
        }
    }
}
