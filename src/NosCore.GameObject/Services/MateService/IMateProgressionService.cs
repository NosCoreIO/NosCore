//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.MateService
{
    public interface IMateProgressionService
    {
        Task AddExperienceAsync(PlayerComponentBundle owner, long experience);

        Task AddExperienceAsync(PlayerComponentBundle owner, Mate mate, long experience);
    }
}
