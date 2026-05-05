//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.QuestService;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    // Capture counterpart to OnEntityDiedHandler: forwards the captor's capture
    // to the quest layer so capture-specific quest objectives (CAPTURE_AND_KEEP /
    // CAPTURE_WITHOUT_KEEPING equivalents) can be progressed.
    [UsedImplicitly]
    public sealed class OnEntityCapturedHandler(IQuestService questService, ILogger<OnEntityCapturedHandler> logger)
    {
        [UsedImplicitly]
        public async Task Handle(EntityCapturedEvent evt)
        {
            if (evt.Captor is not ICharacterEntity character) return;
            if (evt.Victim is not INonPlayableEntity npc || npc.NpcMonster is null) return;

            try
            {
                await questService.OnMonsterCapturedAsync(character, npc.NpcMonster).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to progress capture quest for character {CharacterId}", character.VisualId);
            }
        }
    }
}
