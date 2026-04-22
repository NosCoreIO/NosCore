//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Arch.Core;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

public interface IRewardService
{
    Task DistributeAsync(IAliveEntity victim, IAliveEntity? killer, IReadOnlyDictionary<Entity, int> hitSnapshot);
}
