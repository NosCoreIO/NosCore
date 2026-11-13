//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;

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

        /// <summary>The skills of the card a transformed partner wears, in skill bar order; none when it is not transformed.</summary>
        IReadOnlyList<SkillDto> Skills(PlayerComponentBundle owner, Mate partner);

        /// <summary>Casts the skill in a slot of a transformed partner's skill bar.</summary>
        Task<bool> CastAsync(PlayerComponentBundle owner, long mateTransportId, int slot, IAliveEntity target);
    }
}
