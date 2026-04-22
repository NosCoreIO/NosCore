//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.ExperienceService
{
    public interface IExperienceProgressionService
    {
        Task AddExperienceAsync(PlayerComponentBundle player,
            long levelXpDelta,
            long jobXpDelta,
            long heroXpDelta,
            long spXpDelta,
            int fairyXpDelta);
    }
}
