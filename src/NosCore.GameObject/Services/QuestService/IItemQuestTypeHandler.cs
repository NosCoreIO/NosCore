//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.QuestService;

public interface IItemQuestTypeHandler : IQuestTypeHandler
{
    Task<ItemQuestProgress> OnItemsReceivedAsync(PlayerComponentBundle character, CharacterQuest quest, short vNum);

    Task<ItemQuestProgress> OnItemProducedAsync(PlayerComponentBundle character, CharacterQuest quest, short vNum, short amount);

    Task<ItemQuestProgress> OnItemUsedAsync(PlayerComponentBundle character, CharacterQuest quest, short vNum);

    Task<ItemQuestProgress> OnItemWornAsync(PlayerComponentBundle character, CharacterQuest quest, short vNum);

    Task<ItemQuestProgress> OnNpcDialogRequestedAsync(PlayerComponentBundle character, CharacterQuest quest, short npcVNum);
}
