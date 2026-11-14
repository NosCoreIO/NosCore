//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// No objective: the quest ends on the n_run 200 button of the NPC dialog that offers it.
[UsedImplicitly]
public sealed class NpcButtonQuestHandler : IQuestTypeHandler
{
    public QuestType QuestType => QuestType.UnKnow;

    public Task<bool> OnQuestDialogAnsweredAsync(PlayerComponentBundle character, NpcMonsterDto npc, CharacterQuest quest) =>
        Task.FromResult(true);
}
