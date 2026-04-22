//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Arch.Core;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.QuestService;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    // Bumps kill-count quests (Hunt / NumberOfKill) for each character that
    // contributed damage to the dead NPC. Runs off the same EntityDiedEvent
    // the reward distributor uses so the quest logic never has to be called
    // from BattleService or RewardService directly.
    [UsedImplicitly]
    public sealed class OnEntityDiedHandler(IQuestService questService, ILogger logger)
    {
        [UsedImplicitly]
        public async Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is not INonPlayableEntity npc || npc.NpcMonster is null)
            {
                return;
            }

            var mob = npc.NpcMonster;
            // Process contributors independently so one failure doesn't abort
            // the rest; Wolverine would retry the whole event and double-count
            // the contributors that already succeeded.
            foreach (var (handle, _) in evt.HitSnapshot)
            {
                if (!TryFindCharacter(evt.Victim, handle, out var character))
                {
                    continue;
                }
                try
                {
                    await questService.OnMonsterKilledAsync(character, mob).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Failed to progress kill quest for character {CharacterId}", character.VisualId);
                }
            }
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
