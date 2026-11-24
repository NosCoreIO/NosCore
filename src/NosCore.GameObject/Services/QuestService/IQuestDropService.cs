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
    // timeSpaceId is the client's id of the time-space the monster died in, if any. Each drop names the
    // collector it fell for.
    IReadOnlyList<(short VNum, PlayerComponentBundle Collector)> RollDrops(IReadOnlyList<PlayerComponentBundle> collectors,
        NpcMonsterDto mob, int dropRate, short? timeSpaceId);
}
