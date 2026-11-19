//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.QuestService;

public sealed class QuestCatalog(List<QuestDto> quests, List<QuestObjectiveDto> objectives) : IQuestCatalog, ISingletonService
{
    private readonly FrozenDictionary<int, QuestDto> _quests =
        quests.GroupBy(q => (int)q.QuestId).ToFrozenDictionary(g => g.Key, g => g.First());

    private readonly FrozenDictionary<int, QuestObjectiveDto[]> _objectives =
        objectives.GroupBy(o => (int)o.QuestId).ToFrozenDictionary(g => g.Key, g => g.ToArray());

    public QuestDto? GetQuest(int questId) => _quests.GetValueOrDefault(questId);

    public IReadOnlyList<QuestObjectiveDto> GetObjectives(int questId) =>
        _objectives.TryGetValue(questId, out var list) ? list : [];
}
