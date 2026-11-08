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

// Done once every listed item is carried in its amount; nothing is taken.
[UsedImplicitly]
public sealed class NeededQuestHandler : ItemQuestHandlerBase
{
    public override QuestType QuestType => QuestType.Needed;

    public override Task<ItemQuestProgress> OnItemsReceivedAsync(ICharacterEntity character, CharacterQuest quest, short vNum)
    {
        var objectives = quest.Quest.QuestObjectives;
        if (objectives.All(o => o.ItemVNum(QuestType) != vNum))
        {
            return Task.FromResult(ItemQuestProgress.None);
        }

        var carried = objectives.All(o => o.ItemVNum(QuestType) is { } item
            && CountCarried(character, item) >= System.Math.Max(1, o.RequiredAmount(QuestType)));
        return Task.FromResult(carried ? ItemQuestProgress.Completed : ItemQuestProgress.None);
    }
}
