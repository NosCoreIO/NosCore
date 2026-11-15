//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// The items stay with the character.
[UsedImplicitly]
public sealed class CollectQuestHandler : ItemQuestHandlerBase
{
    public override QuestType QuestType => QuestType.Collect;

    public override Task<ItemQuestProgress> OnItemsReceivedAsync(PlayerComponentBundle character, CharacterQuest quest, short vNum) =>
        Task.FromResult(ProgressToBagCount(character, quest, vNum));
}
