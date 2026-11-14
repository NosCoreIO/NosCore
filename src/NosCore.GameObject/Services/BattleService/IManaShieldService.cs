//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

public interface IManaShieldService
{
    /// <summary>Pays part of the blow from the target's MP and returns what is left for its HP.</summary>
    Task<int> AbsorbAsync(IAliveEntity target, int damage);
}
