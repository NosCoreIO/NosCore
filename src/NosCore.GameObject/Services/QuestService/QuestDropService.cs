//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Helpers;

namespace NosCore.GameObject.Services.QuestService;

public sealed class QuestDropService(INpcCombatCatalog catalog) : IQuestDropService
{
    // The rate is read per 10 000 like monster drops: the rows at 10 000 are bosses that
    // always drop, but rows of 25 items at 100 make for long hunts, so the scale is doubtful.
    private const int ChanceScale = 10000;

    public IReadOnlyList<short> RollDrops(PlayerComponentBundle character, NpcMonsterDto mob, int dropRate)
    {
        var drops = new List<short>();
        var ownDrops = catalog.GetDrops(mob.NpcMonsterVNum).Select(d => (int)d.VNum).ToHashSet();
        foreach (var quest in character.Quests.Values.Where(q => q.ObjectivesCompletedOn is null && q.CompletedOn is null
                     && q.Quest.QuestType is QuestType.Collect or QuestType.CollectInRaid))
        {
            var type = quest.Quest.QuestType;
            foreach (var objective in quest.Quest.QuestObjectives.Where(o => o.FirstData == mob.NpcMonsterVNum))
            {
                if (objective.ItemVNum(type) is not { } vNum
                    || ownDrops.Contains(vNum)
                    || Held(character, quest, objective, vNum) >= objective.RequiredAmount(type)
                    || !Rolls(type, objective, dropRate))
                {
                    continue;
                }

                drops.Add((short)vNum);
            }
        }

        return drops;
    }

    private static int Held(PlayerComponentBundle character, CharacterQuest quest, QuestObjectiveDto objective, int vNum) =>
        quest.Quest.QuestType == QuestType.CollectInRaid
            ? quest.ObjectiveProgress.GetValueOrDefault(objective.QuestObjectiveId)
            : character.InventoryService.CountItem(vNum);

    // A raid collect quest's fourth column repeats the amount rather than giving a chance, so every kill drops one.
    private static bool Rolls(QuestType type, QuestObjectiveDto objective, int dropRate) =>
        type == QuestType.CollectInRaid
        || RandomHelper.Instance.RandomNumber(0, ChanceScale) < (objective.FourthData ?? 0) * dropRate;
}
