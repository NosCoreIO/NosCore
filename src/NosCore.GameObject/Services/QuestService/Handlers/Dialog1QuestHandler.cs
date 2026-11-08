//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// FirstData: the NPC to talk to.
[UsedImplicitly]
public sealed class Dialog1QuestHandler : IQuestTypeHandler
{
    public QuestType QuestType => QuestType.Dialog1;

    public Task<bool> OnNpcTalkedAsync(ICharacterEntity character, NpcMonsterDto npc, CharacterQuest quest) =>
        Task.FromResult(NpcObjectiveProgress.Advance(quest, npc.NpcMonsterVNum, _ => 1)
            && NpcObjectiveProgress.AllReached(quest, _ => 1));
}
