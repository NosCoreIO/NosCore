//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class SummonOnDeathHandler(INpcCombatCatalog catalog, ISummonService summonService)
    {
        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is not MonsterComponentBundle victim || victim.NpcMonster == null || victim.MapInstance == null)
            {
                return Task.CompletedTask;
            }

            return summonService.SummonAsync(victim, catalog.GetDeathBCards(victim.NpcMonster.NpcMonsterVNum));
        }
    }
}
