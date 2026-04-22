//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    // Closes the movement side of the event-driven quest pipeline: on each
    // player step, look at every open GoTo-style quest and ask its handler
    // whether the new position satisfies the target. If yes, hand the quest
    // to QuestService so CompletedOn + the UI packet trio + QuestCompletedEvent
    // all run through the same, ordered path the kill flow uses.
    [UsedImplicitly]
    public sealed class OnCharacterMovedHandler(
        IQuestService questService,
        IEnumerable<IQuestTypeHandler> questTypeHandlers)
    {
        [UsedImplicitly]
        public async Task Handle(CharacterMovedEvent evt)
        {
            foreach (var quest in evt.Character.Quests.Values.Where(q => q.CompletedOn is null).ToList())
            {
                if (quest.Quest.QuestType != QuestType.GoTo)
                {
                    continue;
                }
                var handler = questTypeHandlers.FirstOrDefault(h => h.QuestType == quest.Quest.QuestType);
                if (handler == null || !await handler.ValidateAsync(evt.Character, quest))
                {
                    continue;
                }
                await questService.CompleteQuestAsync(evt.Character, quest);
            }
        }
    }
}
