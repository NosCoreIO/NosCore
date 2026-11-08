//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

public abstract class CaptureQuestHandlerBase : IQuestTypeHandler
{
    public abstract QuestType QuestType { get; }

    public Task<bool> ValidateAsync(ICharacterEntity character, CharacterQuest quest) =>
        Task.FromResult(quest.AreObjectivesComplete());

    public async Task<bool> OnMonsterCapturedAsync(ICharacterEntity character, NpcMonsterDto mob, CharacterQuest quest)
    {
        if (!NpcObjectiveProgress.Advance(quest, mob.NpcMonsterVNum, Required))
        {
            return false;
        }

        if (NpcObjectiveProgress.AllReached(quest, Required))
        {
            return true;
        }

        await character.SendPacketAsync(quest.GenerateQstiPacket(false));
        return false;
    }

    private static int Required(QuestObjectiveDto objective) => objective.SecondData ?? 0;
}
