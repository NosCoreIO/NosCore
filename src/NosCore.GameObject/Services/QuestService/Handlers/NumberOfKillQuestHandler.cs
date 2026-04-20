//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

[UsedImplicitly]
public sealed class NumberOfKillQuestHandler : IQuestTypeHandler
{
    public QuestType QuestType => QuestType.NumberOfKill;

    public async Task OnMonsterKilledAsync(ICharacterEntity character, NpcMonsterDto mob, CharacterQuest quest)
    {
        var progressed = false;
        foreach (var objective in quest.Quest.QuestObjectives)
        {
            if (objective.FirstData != mob.NpcMonsterVNum)
            {
                continue;
            }
            var required = objective.SecondData ?? 0;
            var current = quest.ObjectiveProgress.AddOrUpdate(objective.QuestObjectiveId, 1, (_, e) => e + 1);
            if (required > 0 && current > required)
            {
                quest.ObjectiveProgress[objective.QuestObjectiveId] = required;
            }
            progressed = true;
        }

        if (progressed)
        {
            await character.SendPacketAsync(quest.GenerateQstiPacket(false));
        }
    }
}
