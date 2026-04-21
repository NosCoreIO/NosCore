//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.QuestService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    // Bumps kill-count quests (Hunt / NumberOfKill) for each character that
    // contributed damage to the dead NPC. Runs off the same EntityDiedEvent
    // the reward distributor uses so the quest logic never has to be called
    // from BattleService or RewardService directly.
    [UsedImplicitly]
    public sealed class OnEntityDiedHandler(IQuestService questService)
    {
        [UsedImplicitly]
        public async Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is not INonPlayableEntity npc || npc.NpcMonster is null)
            {
                return;
            }

            var mob = npc.NpcMonster;
            // Snapshot before iterating: RewardDistributionHandler subscribes
            // to the same event and clears HitList at the end of its run;
            // Wolverine doesn't guarantee subscriber ordering, so work off a
            // copy rather than the live dictionary.
            var hits = evt.Victim.HitList.ToArray();
            var tasks = new List<Task>();
            foreach (var (handle, _) in hits)
            {
                if (TryFindCharacter(evt.Victim, handle, out var character))
                {
                    tasks.Add(questService.OnMonsterKilledAsync(character, mob));
                }
            }
            await Task.WhenAll(tasks);
        }

        private static bool TryFindCharacter(IAliveEntity victim, Entity handle, out ICharacterEntity character)
        {
            if (victim.MapInstance is not null)
            {
                var world = victim.MapInstance.EcsWorld;
                if (world.World.IsAlive(handle))
                {
                    var identity = world.TryGetComponent<EntityIdentityComponent>(handle);
                    if (identity is { VisualType: VisualType.Player })
                    {
                        character = new PlayerComponentBundle(handle, world);
                        return true;
                    }
                }
            }
            character = null!;
            return false;
        }
    }
}
