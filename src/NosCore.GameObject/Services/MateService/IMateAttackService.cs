//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.MateService
{
    public interface IMateAttackService
    {
        // The creature's own basic attack, gated by its BasicCooldown and BasicRange.
        Task<bool> BasicAttackAsync(MateComponentBundle mate, IAliveEntity target);
    }
}
