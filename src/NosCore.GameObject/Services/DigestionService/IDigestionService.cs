//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Services.DigestionService;

public interface IDigestionService
{
    Task TickAsync(MapInstance mapInstance);

    Task TickPlayerAsync(PlayerComponentBundle player);
}
