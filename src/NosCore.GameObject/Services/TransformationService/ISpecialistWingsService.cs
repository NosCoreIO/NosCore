//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.TransformationService;

public interface ISpecialistWingsService
{
    // Gives a transformed character the card of the wings their specialist wears, and takes it
    // away once they change wings or leave the transformation.
    Task RefreshAsync(PlayerComponentBundle character);
}
