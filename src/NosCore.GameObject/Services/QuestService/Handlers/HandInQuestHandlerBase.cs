//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// DATA: npc vnum, amount, item vnum. Talking to the NPC while carrying the items hands them all
// in at once; carrying them alone does not count, so a q_pay step cannot pay out before it.
public abstract class HandInQuestHandlerBase : ItemQuestHandlerBase
{
    public override async Task<ItemQuestProgress> OnNpcDialogRequestedAsync(ICharacterEntity character, CharacterQuest quest, short npcVNum)
    {
        var due = quest.Quest.QuestObjectives
            .Where(o => o.FirstData == npcVNum && o.ThirdData != null
                && !(quest.ObjectiveProgress.TryGetValue(o.QuestObjectiveId, out var c) && c >= (o.SecondData ?? 0)))
            .ToList();
        if (due.Count == 0 || due.Any(o => CountCarried(character, o.ThirdData!.Value) < (o.SecondData ?? 0)))
        {
            return ItemQuestProgress.None;
        }

        foreach (var objective in due)
        {
            var amount = objective.SecondData ?? 0;
            await character.SendPacketsAsync(TakeCarried(character, objective.ThirdData!.Value, amount));
            quest.ObjectiveProgress[objective.QuestObjectiveId] = amount;
        }

        return Outcome(quest, true);
    }
}
