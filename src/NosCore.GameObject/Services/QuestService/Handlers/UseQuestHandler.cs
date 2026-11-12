//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using NosCore.GameObject.Ecs;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

[UsedImplicitly]
public sealed class UseQuestHandler : ItemQuestHandlerBase
{
    public override QuestType QuestType => QuestType.Use;

    public override Task<ItemQuestProgress> OnItemUsedAsync(PlayerComponentBundle character, CharacterQuest quest, short vNum) =>
        Task.FromResult(quest.Quest.QuestObjectives.Any(o => o.ItemVNum(QuestType) == vNum)
            ? ItemQuestProgress.Completed
            : ItemQuestProgress.None);
}
