//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.MateService
{
    public interface IPartnerSpecialistService
    {
        /// <summary>
        /// Turns the team partner back when it is transformed, otherwise starts the bar whose end transforms it.
        /// </summary>
        Task ToggleAsync(PlayerComponentBundle owner);

        /// <summary>Transforms the team partner once the bar started by <see cref="ToggleAsync" /> has run out.</summary>
        Task TransformAsync(PlayerComponentBundle owner);

        Task UntransformAsync(PlayerComponentBundle owner, Mate partner);
    }
}
