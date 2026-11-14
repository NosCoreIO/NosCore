//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
    ILogLanguageLocalizer<LogLanguageKey> logLanguage) : KillQuestHandlerBase(logger, logLanguage)
{
    // The objective's first column names a place rather than a monster.
    private const int AnyTimeSpace = 9999;

    public override QuestType QuestType => QuestType.DefeatEnemies;

    protected override bool Counts(PlayerComponentBundle character, NpcMonsterDto mob, QuestObjectiveDto objective) =>
        objective.FirstData == AnyTimeSpace && character.MapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance;
}
