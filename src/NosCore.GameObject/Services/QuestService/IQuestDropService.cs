//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.QuestService;

public interface IQuestDropService
{
    IReadOnlyList<short> RollDrops(PlayerComponentBundle character, NpcMonsterDto mob, int dropRate);
}
