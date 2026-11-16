//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.QuestService;

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    [UsedImplicitly]
    public sealed class OnEntityCapturedHandler(IQuestService questService)
    {
        [UsedImplicitly]
        public async Task Handle(EntityCapturedEvent evt)
        {
            if (evt.Captor is not PlayerComponentBundle character) return;
            if (evt.Victim is not INonPlayableEntity npc || npc.NpcMonster is null) return;

            await questService.OnMonsterCapturedAsync(character, npc.NpcMonster);
        }
    }
}
