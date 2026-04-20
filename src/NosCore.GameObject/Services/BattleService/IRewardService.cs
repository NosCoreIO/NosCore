//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

// Consumes a dead target's HitList and distributes rewards (XP / gold / drops) to each
// attacker proportional to the damage they dealt. Player-only rewards are filtered out
// for monster attackers — a monster killing another monster gives nothing. The HitList
// is cleared here so a re-used entity (respawn) starts fresh.
public interface IRewardService
{
    Task DistributeAsync(IAliveEntity victim, IAliveEntity? killer);
}
