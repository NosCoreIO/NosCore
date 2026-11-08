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

// DATA: item vnum.
[UsedImplicitly]
public sealed class UseQuestHandler : ItemQuestHandlerBase
{
    public override QuestType QuestType => QuestType.Use;

    public override Task<ItemQuestProgress> OnItemUsedAsync(ICharacterEntity character, CharacterQuest quest, short vNum) =>
        Task.FromResult(quest.Quest.QuestObjectives.Any(o => o.FirstData == vNum)
            ? ItemQuestProgress.Completed
            : ItemQuestProgress.None);
}
