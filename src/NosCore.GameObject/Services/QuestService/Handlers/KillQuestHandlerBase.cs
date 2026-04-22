//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;
using Serilog;

namespace NosCore.GameObject.Services.QuestService.Handlers;

public abstract class KillQuestHandlerBase(ILogger logger) : IQuestTypeHandler
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

        // Progress has already been committed to ObjectiveProgress. A qsti send
        // failure must not bubble up — Wolverine would retry the whole kill
        // handler and double-increment the objective.
        try
        {
            await character.SendPacketAsync(quest.GenerateQstiPacket(false));
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to send qsti progress for character {CharacterId} quest {QuestId}",
                character.CharacterId, quest.QuestId);
        }
    }
}
