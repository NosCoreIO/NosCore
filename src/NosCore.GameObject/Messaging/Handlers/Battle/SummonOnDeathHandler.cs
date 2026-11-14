//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class SummonOnDeathHandler(INpcCombatCatalog catalog, List<NpcMonsterDto> npcMonsters, IRandomProvider random)
    {
        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is not MonsterComponentBundle victim || victim.NpcMonster == null || victim.MapInstance == null)
            {
                return Task.CompletedTask;
            }

            var summons = new List<NpcMonsterDto>();
            foreach (var bcard in catalog.GetDeathBCards(victim.NpcMonster.NpcMonsterVNum))
            {
                var count = bcard.Effect() switch
                {
                    BCardEffect.SummonsSummonUponDeath => bcard.FirstData,
                    BCardEffect.SummonsSummonUponDeathChance => random.Next(0, 100) < bcard.FirstData ? 1 : 0,
                    _ => 0,
                };
                if (count <= 0 || npcMonsters.Find(m => m.NpcMonsterVNum == bcard.SecondData) is not { } summoned)
                {
                    continue;
                }

                summons.AddRange(Enumerable.Repeat(summoned, count));
            }

            if (summons.Count == 0)
            {
                return Task.CompletedTask;
            }

            return victim.OnMapAsync(async () =>
            {
                var map = victim.MapInstance;
                foreach (var npcMonster in summons)
                {
                    var monster = map.SpawnMonster(npcMonster, victim.PositionX, victim.PositionY, true);
                    await map.SendPacketAsync(monster.GenerateIn());
                }
            });
        }
    }
}
