//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Services.QuestService.Handlers;

public abstract class KillQuestHandlerBase(IClock clock) : IQuestTypeHandler
{
    public abstract QuestType QuestType { get; }

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

        if (!progressed)
        {
            return;
        }

        await character.SendPacketAsync(quest.GenerateQstiPacket(false));

        if (!IsComplete(quest))
        {
            return;
        }

        quest.CompletedOn = clock.GetCurrentInstant();
        await character.SendPacketAsync(new MsgiPacket
        {
            Type = MessageType.Default,
            Message = Game18NConstString.QuestComplete
        });
        await character.SendPacketAsync(quest.GenerateQstiPacket(false));
        await character.SendPacketAsync(character.GenerateQuestPacket());
    }

    private static bool IsComplete(CharacterQuest quest)
    {
        return quest.Quest.QuestObjectives.All(objective =>
        {
            var required = objective.SecondData ?? 0;
            if (required <= 0)
            {
                return true;
            }
            var current = quest.ObjectiveProgress.TryGetValue(objective.QuestObjectiveId, out var c) ? c : 0;
            return current >= required;
        });
    }
}
