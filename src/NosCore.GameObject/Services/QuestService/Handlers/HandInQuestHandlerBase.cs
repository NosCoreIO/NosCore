//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using NosCore.GameObject.Ecs;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.InventoryService;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// Talking to the NPC while carrying the items hands them all in at once; carrying them alone does
// not count, so a q_pay step cannot pay out before it.
public abstract class HandInQuestHandlerBase : ItemQuestHandlerBase
{
    public override async Task<ItemQuestProgress> OnNpcDialogRequestedAsync(PlayerComponentBundle character, CharacterQuest quest, short npcVNum)
    {
        var due = quest.Quest.QuestObjectives
            .Where(o => o.NpcVNum(QuestType) == npcVNum && o.ItemVNum(QuestType) != null
                && !(quest.ObjectiveProgress.TryGetValue(o.QuestObjectiveId, out var c) && c >= o.RequiredAmount(QuestType)))
            .ToList();
        if (due.Count == 0 || due.Any(o => character.InventoryService.CountItem(o.ItemVNum(QuestType).GetValueOrDefault()) < o.RequiredAmount(QuestType)))
        {
            return ItemQuestProgress.None;
        }

        foreach (var objective in due)
        {
            var amount = objective.RequiredAmount(QuestType);
            var packets = character.InventoryService.RemoveItemAmountByVNum(objective.ItemVNum(QuestType).GetValueOrDefault(), amount);
            quest.ObjectiveProgress[objective.QuestObjectiveId] = amount;
            await character.SendPacketsAsync(packets);
        }

        return Outcome(quest, true);
    }
}
