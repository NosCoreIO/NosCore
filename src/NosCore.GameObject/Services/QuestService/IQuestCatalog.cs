//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.QuestService;

public interface IQuestCatalog
{
    QuestDto? GetQuest(int questId);

    IReadOnlyList<QuestObjectiveDto> GetObjectives(int questId);
}
