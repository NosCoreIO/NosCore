//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.QuestService.Handlers;

[UsedImplicitly]
public sealed class DefeatEnemiesQuestHandler(ILogger<DefeatEnemiesQuestHandler> logger,
    ILogLanguageLocalizer<LogLanguageKey> logLanguage) : KillQuestHandlerBase(logger, logLanguage), IQuestTypeHandler
{
    // The objective's first column names a place rather than a monster.
    private const int AnyTimeSpace = 9999;
    private const int Act4 = 9998;

    public override QuestType QuestType => QuestType.DefeatEnemies;

    protected override bool Counts(PlayerComponentBundle character, NpcMonsterDto mob, QuestObjectiveDto objective) =>
        objective.FirstData == AnyTimeSpace && character.MapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance;

    public async Task<bool> OnEnemyPlayerDefeatedAsync(PlayerComponentBundle character, CharacterQuest quest)
    {
        var counted = false;
        foreach (var objective in quest.Quest.QuestObjectives.Where(o => o.FirstData == Act4))
        {
            var required = objective.SecondData ?? 0;
            if (quest.ObjectiveProgress.TryGetValue(objective.QuestObjectiveId, out var previous) && previous >= required)
            {
                continue;
            }

            quest.ObjectiveProgress[objective.QuestObjectiveId] = previous + 1;
            counted = true;
        }

        if (!counted)
        {
            return false;
        }

        // Read before the await, so only the defeat that met the last objective completes the quest.
        var completed = quest.AreObjectivesComplete();
        await character.SendPacketAsync(quest.GenerateQstiPacket(false));
        return completed;
    }
}
