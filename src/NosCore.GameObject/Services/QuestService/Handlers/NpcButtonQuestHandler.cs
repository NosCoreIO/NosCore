//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// No objective: the quest ends on the n_run 200 button of an NPC dialog. The exception is one
// paired with the quest it links to (that quest links back): it only hands that quest out, so it
// ends as soon as it is taken.
[UsedImplicitly]
public sealed class NpcButtonQuestHandler(List<QuestDto> quests) : IQuestTypeHandler
{
    public QuestType QuestType => QuestType.UnKnow;

    public bool CompletesWhenTaken(Quest quest) =>
        quest.QuestObjectives.Count == 0
        && quest.NextQuestId is { } next
        && quests.Any(q => q.QuestId == next && q.NextQuestId == quest.QuestId);

    public Task<bool> OnQuestDialogAnsweredAsync(PlayerComponentBundle character, NpcMonsterDto npc, CharacterQuest quest) =>
        Task.FromResult(true);
}
