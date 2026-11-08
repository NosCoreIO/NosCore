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

// DATA: item vnum, amount. Done once every listed item is carried in its amount; nothing is taken.
[UsedImplicitly]
public sealed class NeededQuestHandler : ItemQuestHandlerBase
{
    public override QuestType QuestType => QuestType.Needed;

    public override Task<ItemQuestProgress> OnItemsReceivedAsync(ICharacterEntity character, CharacterQuest quest, short vNum)
    {
        var objectives = quest.Quest.QuestObjectives;
        if (objectives.All(o => o.FirstData != vNum))
        {
            return Task.FromResult(ItemQuestProgress.None);
        }

        var carried = objectives.All(o => CountCarried(character, o.FirstData) >= (o.SecondData ?? 1));
        return Task.FromResult(carried ? ItemQuestProgress.Completed : ItemQuestProgress.None);
    }
}
