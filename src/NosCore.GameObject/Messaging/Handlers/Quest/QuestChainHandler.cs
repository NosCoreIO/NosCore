//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    [UsedImplicitly]
    public sealed class QuestChainHandler(IQuestService questService)
    {
        [UsedImplicitly]
        public async Task Handle(QuestCompletedEvent evt)
        {
            if (evt.Quest.Quest.NextQuestId is { } next)
            {
                await questService.AddQuestAsync(evt.Character, QuestActionType.Achieve, next);
            }
        }
    }
}
