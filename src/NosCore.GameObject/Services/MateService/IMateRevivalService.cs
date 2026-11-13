//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Services.MateService
{
    public interface IMateRevivalService
    {
        Task KnockOutAsync(PlayerComponentBundle owner, Mate mate);

        Task TickAsync(MapInstance mapInstance);
    }
}
