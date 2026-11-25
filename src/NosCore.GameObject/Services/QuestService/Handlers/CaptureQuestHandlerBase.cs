//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

public abstract class CaptureQuestHandlerBase : IQuestTypeHandler
{
    public abstract QuestType QuestType { get; }

    protected abstract Game18NConstString ProgressMessage { get; }

    public Task<bool> ValidateAsync(PlayerComponentBundle character, CharacterQuest quest) =>
        Task.FromResult(quest.AreObjectivesComplete());

    public async Task<bool> OnMonsterCapturedAsync(PlayerComponentBundle character, NpcMonsterDto mob, CharacterQuest quest)
    {
        var advanced = false;
        foreach (var objective in quest.Quest.QuestObjectives.Where(o => o.FirstData == mob.NpcMonsterVNum))
        {
            var required = Required(objective);
            var current = quest.ObjectiveProgress.GetValueOrDefault(objective.QuestObjectiveId);
            if (current >= required)
            {
                continue;
            }

            quest.ObjectiveProgress[objective.QuestObjectiveId] = ++current;
            advanced = true;
            await character.SendPacketAsync(character.GenerateSayi2(SayColorType.Red, ProgressMessage, 6, mob.NpcMonsterVNum, $"{current}/{required}"));
        }

        if (!advanced)
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
