//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// Counts each item as it is picked up, since most of these are map items that never reach the bag.
[UsedImplicitly]
public sealed class CollectInRaidQuestHandler : ItemQuestHandlerBase
{
    public override QuestType QuestType => QuestType.CollectInRaid;

    public override Task<ItemQuestProgress> OnItemsReceivedAsync(PlayerComponentBundle character, CharacterQuest quest, short vNum)
    {
        var progressed = false;
        foreach (var objective in quest.Quest.QuestObjectives.Where(o => o.ItemVNum(QuestType) == vNum))
        {
            progressed |= RaiseProgress(quest, objective, quest.ObjectiveProgress.GetValueOrDefault(objective.QuestObjectiveId) + 1);
        }

        return Task.FromResult(Outcome(quest, progressed));
    }
}
