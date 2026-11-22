//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading.Tasks;
using Arch.Core;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.QuestService;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;
using NosCore.Core.Observability;
using System.Collections.Generic;

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    // Bumps kill-count quests (Hunt / NumberOfKill) for each character that contributed damage to the
    // dead NPC and for their party members on its map, whether they hit it or not. It runs after the
    // rewards for the kill.
    [UsedImplicitly]
    [GameEventOrder(1)]
    public sealed class OnEntityDiedHandler(IQuestService questService, ILogger<OnEntityDiedHandler> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        [UsedImplicitly]
        public async Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is not INonPlayableEntity npc || npc.NpcMonster is null)
            {
                return;
            }

            var mob = npc.NpcMonster;
            var counted = new HashSet<long>();
            // Process contributors independently so one failure doesn't abort the rest.
            foreach (var character in Credited(evt))
            {
                if (!counted.Add(character.CharacterId))
                {
                    continue;
                }

                try
                {
                    await questService.OnMonsterKilledAsync(character, mob);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, logLanguage[LogLanguageKey.KILL_QUEST_PROGRESS_FAILED], character.VisualId);
                    NosCoreMetrics.Failures.Add(1, new KeyValuePair<string, object?>("boundary", "game_event"));
                }
            }
        }

        private static List<PlayerComponentBundle> Credited(EntityDiedEvent evt)
        {
            var credited = new List<PlayerComponentBundle>();
            foreach (var (handle, _) in evt.HitSnapshot)
            {
                if (TryFindCharacter(evt.Victim, handle, out var character))
                {
                    credited.AddRange(character.PartyOn(character.World));
                }
            }

            return credited;
        }

        private static bool TryFindCharacter(IAliveEntity victim, Entity handle, out PlayerComponentBundle character)
        {
            if (victim.MapInstance is not null)
            {
                var world = victim.MapInstance.EcsWorld;
                if (world.IsAlive(handle))
                {
                    var identity = world.TryGetComponent<EntityIdentityComponent>(handle);
                    if (identity is { VisualType: VisualType.Player })
                    {
                        character = new PlayerComponentBundle(handle, world);
                        return true;
                    }
                }
            }
            character = default;
            return false;
        }
    }
}
