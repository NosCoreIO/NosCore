//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService;

public static class CaptureQuestObjectives
{
    // True when an open CaptureWithoutGettingTheMonster quest still needs this monster: the capture
    // is handed to the quest and no pet is made.
    public static bool CapturedMonsterGoesToQuest(this ICharacterEntity character, short monsterVNum) =>
        character.Quests.Values.Any(quest => quest.ObjectivesCompletedOn is null && quest.CompletedOn is null
            && quest.Quest.QuestType == QuestType.CaptureWithoutGettingTheMonster
            && quest.Quest.QuestObjectives.Any(o => o.FirstData == monsterVNum
                && quest.ObjectiveProgress.GetValueOrDefault(o.QuestObjectiveId) < (o.SecondData ?? 0)));
}
