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
    public interface IPetSkillService
    {
        /// <summary>Casts one of the skills of a team pet's skill bar, named by its vnum.</summary>
        Task<bool> CastAsync(PlayerComponentBundle owner, long mateTransportId, short skillVNum, IAliveEntity target);
    }
}
