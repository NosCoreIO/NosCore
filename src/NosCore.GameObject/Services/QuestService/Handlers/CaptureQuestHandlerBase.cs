//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

public abstract class CaptureQuestHandlerBase : IQuestTypeHandler
{
    public abstract QuestType QuestType { get; }

    public Task<bool> ValidateAsync(PlayerComponentBundle character, CharacterQuest quest) =>
        Task.FromResult(quest.AreObjectivesComplete());

    public Task<bool> OnMonsterCapturedAsync(PlayerComponentBundle character, NpcMonsterDto mob, CharacterQuest quest) =>
        NpcObjectiveProgress.RecordAsync(character, quest, mob.NpcMonsterVNum, Required);

    private static int Required(QuestObjectiveDto objective) => objective.SecondData ?? 0;
}
