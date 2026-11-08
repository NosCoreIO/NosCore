//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// DATA: item vnum, amount. Only items made by production count, not ones picked up or bought.
[UsedImplicitly]
public sealed class ProductQuestHandler : ItemQuestHandlerBase
{
    public override QuestType QuestType => QuestType.Product;

    public override Task<ItemQuestProgress> OnItemProducedAsync(ICharacterEntity character, CharacterQuest quest, short vNum, short amount)
    {
        var progressed = false;
        foreach (var objective in quest.Quest.QuestObjectives.Where(o => o.FirstData == vNum))
        {
            var previous = quest.ObjectiveProgress.TryGetValue(objective.QuestObjectiveId, out var p) ? p : 0;
            progressed |= RaiseProgress(quest, objective, previous + amount);
        }

        return Task.FromResult(Outcome(quest, progressed));
    }
}
