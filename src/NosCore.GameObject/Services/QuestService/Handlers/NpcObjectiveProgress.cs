//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// Progress for objectives whose FirstData names an NPC. These quest types are not
// counted in qsti, so the tally lives in ObjectiveProgress only to know when every
// target has been reached.
internal static class NpcObjectiveProgress
{
    public static bool Advance(CharacterQuest quest, short npcVNum, Func<QuestObjectiveDto, int> target)
    {
        var advanced = false;
        foreach (var objective in quest.Quest.QuestObjectives.Where(o => o.FirstData == npcVNum))
        {
            var required = target(objective);
            var current = quest.ObjectiveProgress.GetValueOrDefault(objective.QuestObjectiveId);
            if (current >= required)
            {
                continue;
            }

            quest.ObjectiveProgress[objective.QuestObjectiveId] = current + 1;
            advanced = true;
        }

        return advanced;
    }

    public static bool AllReached(CharacterQuest quest, Func<QuestObjectiveDto, int> target) =>
        quest.Quest.QuestObjectives.All(o => quest.ObjectiveProgress.GetValueOrDefault(o.QuestObjectiveId) >= target(o));
}
