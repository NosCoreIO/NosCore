//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.RainbowBattleService;

namespace NosCore.GameObject.Messaging.Handlers.RainbowBattle
{
    [UsedImplicitly]
    public sealed class RainbowCrystalTalkedHandler(IRainbowBattleService rainbowBattleService)
    {
        [UsedImplicitly]
        public Task Handle(NpcTalkedEvent evt)
        {
            return evt.Character is PlayerComponentBundle player && evt.Npc is NpcComponentBundle crystal
                ? rainbowBattleService.StartCaptureAsync(player, crystal)
                : Task.CompletedTask;
        }
    }
}
