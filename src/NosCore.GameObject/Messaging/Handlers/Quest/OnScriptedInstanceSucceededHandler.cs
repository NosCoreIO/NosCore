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

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    [UsedImplicitly]
    public sealed class OnScriptedInstanceSucceededHandler(
        IQuestService questService,
        IEnumerable<IQuestTypeHandler> questTypeHandlers)
    {
        [UsedImplicitly]
        public async Task Handle(ScriptedInstanceSucceededEvent evt)
        {
            foreach (var quest in evt.Character.Quests.Values.Where(q => q.ObjectivesCompletedOn is null && q.CompletedOn is null).ToList())
            {
                var handler = questTypeHandlers.FirstOrDefault(h => h.QuestType == quest.Quest.QuestType);
                if (handler != null && await handler.OnScriptedInstanceSucceededAsync(evt.Character, evt.Type, evt.InstanceId, quest))
                {
                    await questService.CompleteQuestAsync(evt.Character, quest);
                }
            }
        }
    }
}
