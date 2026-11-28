//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
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

    public override async Task<ItemQuestProgress> OnItemsReceivedAsync(PlayerComponentBundle character, CharacterQuest quest, short vNum)
    {
        var progressed = false;
        foreach (var objective in quest.Quest.QuestObjectives.Where(o => o.ItemVNum(QuestType) == vNum))
        {
            if (!RaiseProgress(quest, objective, character.InventoryService.CountItem(vNum)))
            {
                continue;
            }

            progressed = true;
            await character.SendPacketAsync(ProgressLine(character, Game18NConstString.Collected, quest, objective));
        }

        return Outcome(quest, progressed);
    }
}
