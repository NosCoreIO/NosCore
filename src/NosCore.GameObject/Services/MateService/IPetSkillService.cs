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
    public interface IPetSkillService
    {
        /// <summary>The skills of a pet's skill bar, in slot order: its creature's own; none for a partner.</summary>
        IReadOnlyList<SkillDto> Skills(Mate pet);

        /// <summary>Casts one of the skills of a team pet's skill bar, named by its vnum.</summary>
        Task<bool> CastAsync(PlayerComponentBundle owner, long mateTransportId, int skillVNum, IAliveEntity target);
    }
}
